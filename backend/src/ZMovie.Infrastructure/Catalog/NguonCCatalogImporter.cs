using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed record NguonCCatalogImportOptions(int? MaxPages, int StartPage, bool IncludeEpisodes, TimeSpan RequestDelay)
{
    public int DetailConcurrency { get; init; } = 3;
    public static readonly NguonCCatalogImportOptions Default = new(null, 1, true, TimeSpan.FromMilliseconds(300));
}

public sealed record NguonCCatalogImportResult(int TotalItems, int PagesImported, int TitlesImported, int EpisodesImported);

public static partial class NguonCCatalogImporter
{
    private const string BaseUrl = "https://phim.nguonc.com/api";
    private static readonly Regex Html = HtmlRegex();
    private static readonly Regex Minutes = MinutesRegex();
    private static readonly Regex YearRegex = ExtractYearRegex();

    public static async Task<NguonCCatalogImportResult> ImportAsync(
        CatalogDbContext db,
        HttpClient http,
        NguonCCatalogImportOptions options,
        Action<string>? report,
        CancellationToken ct) =>
        await ImportAsync(db, http, options, TimeProvider.System, report, ct);

    public static async Task<NguonCCatalogImportResult> ImportAsync(
        CatalogDbContext db,
        HttpClient http,
        NguonCCatalogImportOptions options,
        TimeProvider timeProvider,
        Action<string>? report,
        CancellationToken ct)
    {
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var first = await GetListPageAsync(http, 1, ct);
        if (first is null || !string.Equals(first.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Failed to load initial page from NguonC API.");
        }

        var totalItems = first.Paginate.TotalItems;
        var totalPages = first.Paginate.TotalPage > 0
            ? first.Paginate.TotalPage
            : (int)Math.Ceiling(totalItems / (double)Math.Max(1, first.Paginate.ItemsPerPage));

        var startPage = totalPages == 0 ? 1 : Math.Clamp(options.StartPage, 1, totalPages);
        var pagesAvailable = Math.Max(0, totalPages - startPage + 1);
        var pagesToImport = Math.Min(options.MaxPages ?? pagesAvailable, pagesAvailable);
        var endPage = startPage + pagesToImport - 1;
        var titlesImported = 0;
        var episodesImported = 0;

        for (var page = startPage; page <= endPage; page++)
        {
            var source = page == 1 ? first : await GetListPageAsync(http, page, ct);
            if (source is null || source.Items is null || source.Items.Count == 0) continue;

            var movies = source.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.Slug) && TitleSlug.TryCreate(x.Slug, out _))
                .GroupBy(x => x.Slug, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

            var parsedSlugs = movies.Select(x => TitleSlug.Parse(x.Slug)).ToList();
            var existingTitles = await db.Titles.Where(x => parsedSlugs.Contains(x.Slug)).ToDictionaryAsync(x => x.Slug.Value, StringComparer.OrdinalIgnoreCase, ct);
            var titlesBySlug = new Dictionary<string, Title>(StringComparer.OrdinalIgnoreCase);
            var now = timeProvider.GetUtcNow();

            foreach (var movie in movies)
            {
                var title = UpsertTitle(db, existingTitles, movie, now);
                titlesBySlug[movie.Slug] = title;
                titlesImported++;
            }

            if (options.IncludeEpisodes)
            {
                episodesImported += await ImportDetailsAsync(db, http, movies, titlesBySlug, options, timeProvider, ct);
            }

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            report?.Invoke($"NguonC catalog: page {page}/{totalPages} ({titlesImported} titles, {episodesImported} episodes)");
            if (page < endPage) await Task.Delay(options.RequestDelay, ct);
        }

        return new(totalItems, pagesToImport, titlesImported, episodesImported);
    }

    private static async Task<int> ImportDetailsAsync(
        CatalogDbContext db,
        HttpClient http,
        IReadOnlyList<NguonCMovieSummary> movies,
        IReadOnlyDictionary<string, Title> titlesBySlug,
        NguonCCatalogImportOptions options,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var titleIds = titlesBySlug.Values.Select(x => x.Id).ToList();
        var existingEpisodes = (await db.Episodes.Where(x => titleIds.Contains(x.TitleId)).ToListAsync(ct))
            .GroupBy(x => x.TitleId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(e => e.Number));

        var episodesCount = 0;
        using var throttle = new SemaphoreSlim(Math.Clamp(options.DetailConcurrency, 1, 8));

        var detailTasks = movies.Select(async movie =>
        {
            if (!titlesBySlug.TryGetValue(movie.Slug, out var title)) return 0;
            await throttle.WaitAsync(ct);
            try
            {
                var detail = await GetDetailAsync(http, movie.Slug, ct);
                if (detail?.Movie?.Episodes is null || detail.Movie.Episodes.Count == 0) return 0;

                // Pick the first server or preferred server
                var server = detail.Movie.Episodes.FirstOrDefault();
                if (server?.Items is null) return 0;

                var serverEpisodes = 0;
                var episodeIndex = 1;
                foreach (var item in server.Items)
                {
                    var streamUrl = !string.IsNullOrWhiteSpace(item.M3u8) ? item.M3u8 : item.Embed;
                    if (string.IsNullOrWhiteSpace(streamUrl)) continue;

                    var episodeNum = ExtractEpisodeNumber(item.Slug, item.Name, episodeIndex);
                    existingEpisodes.TryGetValue(title.Id, out var byNumber);

                    if (byNumber is not null && byNumber.TryGetValue(episodeNum, out var existing))
                    {
                        existing.Update(item.Name ?? $"Tập {episodeNum}", streamUrl, existing.SubtitleUrl);
                    }
                    else
                    {
                        var created = Episode.Create(
                            EpisodeId.New(),
                            title.Id,
                            episodeNum,
                            item.Name ?? $"Tập {episodeNum}",
                            streamUrl,
                            string.Empty);
                        db.Episodes.Add(created);
                    }

                    serverEpisodes++;
                    episodeIndex++;
                }

                return serverEpisodes;
            }
            catch
            {
                // Detail request failures should not abort overall import
                return 0;
            }
            finally
            {
                throttle.Release();
            }
        });

        var results = await Task.WhenAll(detailTasks);
        episodesCount = results.Sum();
        return episodesCount;
    }

    private static Title UpsertTitle(
        CatalogDbContext db,
        IReadOnlyDictionary<string, Title> existingTitles,
        NguonCMovieSummary movie,
        DateTimeOffset now)
    {
        var slug = TitleSlug.Parse(movie.Slug);
        var vietnameseName = (movie.Name ?? string.Empty).Trim();
        var englishName = string.IsNullOrWhiteSpace(movie.OriginalName) ? vietnameseName : movie.OriginalName.Trim();
        var synopsis = Clean(movie.Description);
        var poster = PickPoster(movie.PosterUrl, movie.ThumbUrl);
        var runtime = ParseRuntime(movie.Time);
        var year = ParseYear(movie);
        var (type, genre, country) = ParseCategories(movie.Category);

        if (existingTitles.TryGetValue(movie.Slug, out var existing))
        {
            existing.UpdateMetadata(
                new LocalizedText(vietnameseName, englishName),
                new LocalizedText(synopsis, synopsis),
                genre,
                ReleaseYear.FromInt(year),
                TitleType.Normalize(type),
                poster,
                Runtime.FromMinutes(runtime),
                featured: existing.Featured,
                now,
                actors: movie.Casts ?? string.Empty,
                directors: movie.Director ?? string.Empty,
                country: country,
                trailerUrl: existing.TrailerUrl,
                isR2Hosted: existing.IsR2Hosted);

            return existing;
        }

        var created = Title.Create(
            TitleId.New(),
            slug,
            new LocalizedText(vietnameseName, englishName),
            new LocalizedText(synopsis, synopsis),
            genre,
            ReleaseYear.FromInt(year),
            TitleType.Normalize(type),
            poster,
            Runtime.FromMinutes(runtime),
            featured: false,
            now,
            actors: movie.Casts ?? string.Empty,
            directors: movie.Director ?? string.Empty,
            country: country,
            trailerUrl: string.Empty,
            isR2Hosted: false);

        db.Titles.Add(created);
        return created;
    }

    private static (string Type, string Genre, string Country) ParseCategories(Dictionary<string, NguonCCategoryGroup>? categories)
    {
        var type = "movie";
        var genreList = new List<string>();
        var country = "Việt Nam";

        if (categories is null) return (type, "Phim", country);

        if (categories.TryGetValue("1", out var formatGroup) && formatGroup.List is not null)
        {
            var isSeries = formatGroup.List.Any(x => x.Name?.Contains("bộ", StringComparison.OrdinalIgnoreCase) == true);
            if (isSeries) type = "series";
        }

        if (categories.TryGetValue("2", out var genreGroup) && genreGroup.List is not null)
        {
            genreList.AddRange(genreGroup.List.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x))!);
        }

        if (categories.TryGetValue("3", out var countryGroup) && countryGroup.List is not null)
        {
            var firstCountry = countryGroup.List.FirstOrDefault()?.Name;
            if (!string.IsNullOrWhiteSpace(firstCountry)) country = firstCountry;
        }

        var genre = genreList.Count > 0 ? string.Join(", ", genreList) : "Hành Động, Kịch Tính";
        return (type, genre, country);
    }

    private static int ParseYear(NguonCMovieSummary movie)
    {
        if (movie.Category is not null && movie.Category.TryGetValue("4", out var yearGroup) && yearGroup.List is not null)
        {
            var yearName = yearGroup.List.FirstOrDefault()?.Name;
            if (int.TryParse(yearName, out var parsedYear) && parsedYear is >= 1900 and <= 2100)
            {
                return parsedYear;
            }
        }

        var match = YearRegex.Match(movie.Name ?? string.Empty);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var matchedYear))
        {
            return matchedYear;
        }

        return DateTime.UtcNow.Year;
    }

    private static int ParseRuntime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 90;
        var match = Minutes.Match(text);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mins) && mins > 0
            ? mins
            : 90;
    }

    private static int ExtractEpisodeNumber(string? slug, string? name, int fallback)
    {
        var target = $"{slug} {name}";
        var match = Regex.Match(target, @"(?:tap|tập|ep|episode)[\s\-_]*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var num) && num > 0)
        {
            return num;
        }
        return fallback;
    }

    private static string PickPoster(string? poster, string? thumb)
    {
        if (!string.IsNullOrWhiteSpace(poster) && Uri.IsWellFormedUriString(poster, UriKind.Absolute)) return poster;
        if (!string.IsNullOrWhiteSpace(thumb) && Uri.IsWellFormedUriString(thumb, UriKind.Absolute)) return thumb;
        return "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1200&q=80";
    }

    private static string Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Thông tin đang được cập nhật.";
        var withoutTags = Html.Replace(raw, string.Empty);
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return string.IsNullOrWhiteSpace(decoded) ? "Thông tin đang được cập nhật." : decoded.Trim();
    }

    private static async Task<NguonCListResponse?> GetListPageAsync(HttpClient http, int page, CancellationToken ct)
    {
        return await http.GetFromJsonAsync<NguonCListResponse>($"{BaseUrl}/films/phim-moi-cap-nhat?page={page}", ct);
    }

    private static async Task<NguonCDetailResponse?> GetDetailAsync(HttpClient http, string slug, CancellationToken ct)
    {
        return await http.GetFromJsonAsync<NguonCDetailResponse>($"{BaseUrl}/film/{slug}", ct);
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlRegex();

    [GeneratedRegex(@"(\d+)\s*(?:phút|min|m|p)", RegexOptions.IgnoreCase)]
    private static partial Regex MinutesRegex();

    [GeneratedRegex(@"\b(19\d{2}|20\d{2})\b")]
    private static partial Regex ExtractYearRegex();
}

public sealed record NguonCListResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("paginate")] NguonCPaginate Paginate,
    [property: JsonPropertyName("items")] List<NguonCMovieSummary> Items
);

public sealed record NguonCPaginate(
    [property: JsonPropertyName("current_page")] int CurrentPage,
    [property: JsonPropertyName("total_page")] int TotalPage,
    [property: JsonPropertyName("total_items")] int TotalItems,
    [property: JsonPropertyName("items_per_page")] int ItemsPerPage
);

public sealed record NguonCMovieSummary(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("original_name")] string OriginalName,
    [property: JsonPropertyName("thumb_url")] string ThumbUrl,
    [property: JsonPropertyName("poster_url")] string PosterUrl,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("time")] string Time,
    [property: JsonPropertyName("director")] string Director,
    [property: JsonPropertyName("casts")] string Casts,
    [property: JsonPropertyName("category")] Dictionary<string, NguonCCategoryGroup> Category
);

public sealed record NguonCCategoryGroup(
    [property: JsonPropertyName("group")] NguonCCategoryGroupInfo Group,
    [property: JsonPropertyName("list")] List<NguonCCategoryItem> List
);

public sealed record NguonCCategoryGroupInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public sealed record NguonCCategoryItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public sealed record NguonCDetailResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("movie")] NguonCDetailMovie Movie
);

public sealed record NguonCDetailMovie(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("episodes")] List<NguonCServerGroup> Episodes
);

public sealed record NguonCServerGroup(
    [property: JsonPropertyName("server_name")] string ServerName,
    [property: JsonPropertyName("items")] List<NguonCServerItem> Items
);

public sealed record NguonCServerItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("embed")] string Embed,
    [property: JsonPropertyName("m3u8")] string M3u8
);
