using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed record OPhimCatalogImportOptions(int? MaxPages, int StartPage, bool IncludeEpisodes, TimeSpan RequestDelay)
{
    public int DetailConcurrency { get; init; } = 3;
    public static readonly OPhimCatalogImportOptions FullMetadata = new(null, 1, false, TimeSpan.FromMilliseconds(300));
}

public sealed record OPhimCatalogImportResult(int TotalItems, int PagesImported, int TitlesImported, int EpisodesImported);

/// <summary>Imports OPhim's public catalog into the fields that ZMovie currently uses.</summary>
public static partial class OPhimCatalogImporter
{
    private const string BaseUrl = "https://ophim1.com/v1/api";
    private static readonly Regex Html = HtmlRegex();
    private static readonly Regex Minutes = MinutesRegex();

    public static async Task<OPhimCatalogImportResult> ImportAsync(
        CatalogDbContext db,
        HttpClient http,
        OPhimCatalogImportOptions options,
        Action<string>? report,
        CancellationToken ct)
    {
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var first = await GetListPageAsync(http, 1, ct);
        EnsureSuccess(first.Status, first.Message);
        var totalItems = first.Data.Params.Pagination.TotalItems;
        var totalPages = (int)Math.Ceiling(totalItems / (double)Math.Max(1, first.Data.Params.Pagination.TotalItemsPerPage));
        var startPage = Math.Clamp(options.StartPage, 1, totalPages);
        var pagesAvailable = totalPages - startPage + 1;
        var pagesToImport = Math.Min(options.MaxPages ?? pagesAvailable, pagesAvailable);
        var endPage = startPage + pagesToImport - 1;
        var titlesImported = 0;
        var episodesImported = 0;

        for (var page = startPage; page <= endPage; page++)
        {
            var source = page == 1 ? first : await GetListPageAsync(http, page, ct);
            EnsureSuccess(source.Status, source.Message);
            var movies = source.Data.Items.Where(x => !string.IsNullOrWhiteSpace(x.Slug)).ToList();
            var slugs = movies.Select(x => x.Slug).ToList();
            var existingTitles = await db.Titles.Where(x => slugs.Contains(x.Slug)).ToDictionaryAsync(x => x.Slug, ct);
            var titlesBySlug = new Dictionary<string, CatalogTitle>(StringComparer.OrdinalIgnoreCase);
            foreach (var movie in movies)
            {
                var title = UpsertTitle(db, existingTitles, movie, source.Data.ImageCdn);
                titlesBySlug[movie.Slug] = title;
                titlesImported++;
            }

            if (options.IncludeEpisodes)
            {
                episodesImported += await ImportDetailsAsync(db, http, movies, titlesBySlug, options, ct);
            }

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            report?.Invoke($"OPhim catalog: page {page}/{totalPages} ({titlesImported} titles, {episodesImported} episodes)");
            if (page < endPage) await DelayAsync(options.RequestDelay, ct);
        }

        return new(totalItems, pagesToImport, titlesImported, episodesImported);
    }

    private static async Task<int> ImportDetailsAsync(
        CatalogDbContext db,
        HttpClient http,
        IReadOnlyList<OPhimMovie> movies,
        IReadOnlyDictionary<string, CatalogTitle> titlesBySlug,
        OPhimCatalogImportOptions options,
        CancellationToken ct)
    {
        using var concurrencyGate = new SemaphoreSlim(Math.Clamp(options.DetailConcurrency, 1, 8));
        var detailTasks = movies.Select(async movie =>
        {
            await concurrencyGate.WaitAsync(ct);
            try
            {
                await DelayAsync(options.RequestDelay, ct);
                var detail = await GetDetailAsync(http, movie.Slug, ct);
                EnsureSuccess(detail.Status, detail.Message);
                return new FetchedDetail(movie.Slug, detail.Data.Item.Content, detail.Data.Item.Episodes);
            }
            finally
            {
                concurrencyGate.Release();
            }
        }).ToArray();

        var details = await Task.WhenAll(detailTasks);
        var episodesImported = 0;
        foreach (var detail in details)
        {
            if (!titlesBySlug.TryGetValue(detail.Slug, out var title)) continue;
            ApplySynopsis(title, detail.Content);
            episodesImported += await UpsertEpisodesAsync(db, title, detail.Episodes, ct);
        }

        return episodesImported;
    }

    private static CatalogTitle UpsertTitle(CatalogDbContext db, IReadOnlyDictionary<string, CatalogTitle> existingTitles, OPhimMovie source, string? imageCdn)
    {
        existingTitles.TryGetValue(source.Slug, out var title);
        var vietnameseTitle = Limit(source.Name, 300, source.Slug);
        var englishTitle = Limit(source.OriginName, 300, vietnameseTitle);
        var genre = Limit(string.Join(", ", (source.Category ?? []).Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x))), 100, "Khác");
        var poster = BuildImageUrl(imageCdn, source.PosterUrl, source.ThumbUrl);
        if (title is null)
        {
            title = new CatalogTitle
            {
                Slug = source.Slug,
                EnglishTitle = englishTitle,
                VietnameseTitle = vietnameseTitle,
                EnglishSynopsis = string.Empty,
                VietnameseSynopsis = string.Empty,
                Genre = genre,
                Year = source.Year ?? 0,
                Type = ToZMovieType(source.Type),
                PosterUrl = poster,
                RuntimeMinutes = ParseMinutes(source.Time),
                Featured = false
            };
            db.Titles.Add(title);
            return title;
        }

        title.EnglishTitle = englishTitle;
        title.VietnameseTitle = vietnameseTitle;
        title.Genre = genre;
        title.Year = source.Year ?? 0;
        title.Type = ToZMovieType(source.Type);
        title.PosterUrl = poster;
        title.RuntimeMinutes = ParseMinutes(source.Time);
        title.UpdatedAt = DateTimeOffset.UtcNow;
        return title;
    }

    private static async Task<int> UpsertEpisodesAsync(CatalogDbContext db, CatalogTitle title, IReadOnlyList<OPhimServer> servers, CancellationToken ct)
    {
        var existing = await db.Episodes.Where(x => x.TitleId == title.Id).ToDictionaryAsync(x => x.Number, ct);
        var addedOrUpdated = 0;
        var ordinal = 0;
        foreach (var source in servers.SelectMany(x => x.ServerData))
        {
            if (string.IsNullOrWhiteSpace(source.LinkM3u8)) continue;
            ordinal++;
            var number = int.TryParse(source.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : ordinal;
            if (!existing.TryGetValue(number, out var episode))
            {
                episode = new CatalogEpisode { TitleId = title.Id, Number = number, Name = Limit(source.Name, 300, $"Tập {number}"), HlsUrl = Limit(source.LinkM3u8, 2000, string.Empty) };
                db.Episodes.Add(episode);
                existing[number] = episode;
            }
            else
            {
                episode.Name = Limit(source.Name, 300, $"Tập {number}");
                episode.HlsUrl = Limit(source.LinkM3u8, 2000, string.Empty);
            }
            addedOrUpdated++;
        }
        return addedOrUpdated;
    }

    private static void ApplySynopsis(CatalogTitle title, string? content)
    {
        var synopsis = Limit(Html.Replace(content ?? string.Empty, " "), 4000, string.Empty);
        title.VietnameseSynopsis = synopsis;
        if (string.IsNullOrWhiteSpace(title.EnglishSynopsis)) title.EnglishSynopsis = synopsis;
        title.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static Task<OPhimListResponse> GetListPageAsync(HttpClient http, int page, CancellationToken ct) =>
        GetJsonWithRetryAsync<OPhimListResponse>(http, $"{BaseUrl}/danh-sach/phim-moi-cap-nhat?page={page}", ct);

    private static Task<OPhimDetailResponse> GetDetailAsync(HttpClient http, string slug, CancellationToken ct) =>
        GetJsonWithRetryAsync<OPhimDetailResponse>(http, $"{BaseUrl}/phim/{Uri.EscapeDataString(slug)}", ct);

    private static async Task<T> GetJsonWithRetryAsync<T>(HttpClient http, string url, CancellationToken ct)
    {
        const int maxAttempts = 4;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>(ct)
                        ?? throw new InvalidOperationException("OPhim returned no JSON payload.");
                }

                if (!IsTransient(response.StatusCode) || attempt == maxAttempts - 1)
                {
                    response.EnsureSuccessStatusCode();
                }

                var serverDelay = response.Headers.RetryAfter?.Delta;
                await DelayAsync(serverDelay is { } retryAfter && retryAfter > TimeSpan.Zero
                    ? retryAfter
                    : TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))), ct);
            }
            catch (HttpRequestException) when (attempt < maxAttempts - 1)
            {
                await DelayAsync(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))), ct);
            }
        }

        throw new InvalidOperationException("OPhim request failed after retries.");
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode) =>
        statusCode is System.Net.HttpStatusCode.RequestTimeout
            or System.Net.HttpStatusCode.TooManyRequests
            or System.Net.HttpStatusCode.InternalServerError
            or System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout;

    private static async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
    }

    private static void EnsureSuccess(string status, string? message)
    {
        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"OPhim request failed: {message ?? status}");
    }

    private static string ToZMovieType(string? sourceType) => string.Equals(sourceType, "single", StringComparison.OrdinalIgnoreCase) ? "movie" : "series";
    private static int ParseMinutes(string? value) => int.TryParse(Minutes.Match(value ?? string.Empty).Value, out var minutes) ? minutes : 0;
    private static string BuildImageUrl(string? cdn, string? primaryPath, string? fallbackPath)
    {
        var primary = BuildCandidateImageUrl(cdn, primaryPath);
        if (primary.Length <= 2000) return primary;

        var fallback = BuildCandidateImageUrl(cdn, fallbackPath);
        return fallback.Length <= 2000 ? fallback : string.Empty;
    }

    private static string BuildCandidateImageUrl(string? cdn, string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? path
                : $"{cdn?.TrimEnd('/') ?? "https://img.ophim.live"}/uploads/movies/{path.TrimStart('/')}";
    private static string Limit(string? value, int maxLength, string fallback) => (value ?? fallback).Trim() switch { "" => fallback, var text => text.Length <= maxLength ? text : text[..maxLength] };

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlRegex();
    [GeneratedRegex("\\d+", RegexOptions.Compiled)]
    private static partial Regex MinutesRegex();

    private sealed record OPhimListResponse(string Status, string? Message, OPhimListData Data);
    private sealed record OPhimListData(OPhimListParams Params, IReadOnlyList<OPhimMovie> Items, [property: JsonPropertyName("APP_DOMAIN_CDN_IMAGE")] string? APP_DOMAIN_CDN_IMAGE)
    {
        public string? ImageCdn => APP_DOMAIN_CDN_IMAGE;
    }
    private sealed record OPhimListParams(OPhimPagination Pagination);
    private sealed record OPhimPagination(int TotalItems, int TotalItemsPerPage);
    private sealed record OPhimDetailResponse(string Status, string? Message, OPhimDetailData Data);
    private sealed record OPhimDetailData(OPhimDetailItem Item);
    private sealed record OPhimDetailItem(string? Content, IReadOnlyList<OPhimServer> Episodes);
    private sealed record OPhimMovie(string Slug, string? Name, [property: JsonPropertyName("origin_name")] string? OriginName, string? Type, int? Year, string? Time, [property: JsonPropertyName("poster_url")] string? PosterUrl, [property: JsonPropertyName("thumb_url")] string? ThumbUrl, IReadOnlyList<OPhimCategory>? Category);
    private sealed record OPhimCategory(string Name);
    private sealed record OPhimServer([property: JsonPropertyName("server_data")] IReadOnlyList<OPhimEpisode> ServerData);
    private sealed record OPhimEpisode(string? Name, [property: JsonPropertyName("link_m3u8")] string? LinkM3u8);
    private sealed record FetchedDetail(string Slug, string? Content, IReadOnlyList<OPhimServer> Episodes);
}
