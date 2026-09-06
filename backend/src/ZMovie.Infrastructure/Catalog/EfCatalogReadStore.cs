using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Analytics;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfCatalogReadStore(CatalogDbContext db, IViewAnalyticsQueries analytics) : ICatalogReadStore
{
    private const string NatraHeroBannerUrl = "https://cdnstatic.usheru.com/img/movies/original_8btfz81bOJ2lC7cujYBTw03wzg3.jpg";

    public async Task<TitleListResponse> ListAsync(
        string? query,
        string? genre,
        string? country,
        int? year,
        string? type,
        string? sort,
        int page,
        int pageSize,
        string locale,
        CancellationToken ct)
    {
        var titles = db.Titles.AsNoTracking().AsQueryable();
        var q = query?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            titles = titles.Where(x =>
                x.EnglishTitle.Contains(q) ||
                x.VietnameseTitle.Contains(q) ||
                x.Genre.Contains(q) ||
                x.Actors.Contains(q));
        }
        if (!string.IsNullOrWhiteSpace(genre))
        {
            titles = titles.Where(x => EF.Functions.ILike(x.Genre, $"%{genre.Trim()}%"));
        }
        if (!string.IsNullOrWhiteSpace(country))
        {
            titles = titles.Where(x => EF.Functions.ILike(x.Country, country.Trim()));
        }
        if (year.HasValue)
        {
            var selectedYear = ReleaseYear.FromInt(year.Value);
            titles = titles.Where(x => x.Year == selectedYear);
        }
        titles = type?.Trim().ToLowerInvariant() switch
        {
            "movie" => titles.Where(x => x.Type == TitleType.Movie),
            "series" => titles.Where(x => x.Type == TitleType.Series),
            "r2" => titles.Where(x => x.IsR2Hosted),
            _ => titles,
        };

        var total = await titles.CountAsync(ct);
        titles = sort?.Trim().ToLowerInvariant() switch
        {
            "oldest" => titles.OrderBy(x => x.Year).ThenBy(x => x.VietnameseTitle),
            "title" => titles.OrderBy(x => locale == "en" ? x.EnglishTitle : x.VietnameseTitle),
            _ => titles.OrderByDescending(x => x.Featured).ThenByDescending(x => x.Year),
        };
        var items = await titles
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new(items.Select(x => Summary(x, locale)).ToList(), total);
    }

    public async Task<TitleDetail?> GetAsync(string slug, string locale, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        var title = await db.Titles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == titleSlug, ct);
        if (title is null) return null;
        return Detail(title, locale, await analytics.GetViewCountAsync(new AnalyticsTitleId(title.Id.Value), ct));
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct)
    {
        var imported = await db.Genres.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(ct);
        return imported.Count > 0 ? imported : await db.Titles.AsNoTracking().Select(x => x.Genre).Distinct().Order().ToListAsync(ct);
    }

    public async Task<PlaybackResponse?> GetPlaybackAsync(string slug, string locale, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        var title = await db.Titles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == titleSlug, ct);
        if (title is null) return null;

        var episodes = await db.Episodes.AsNoTracking()
            .Include(x => x.Sources)
            .Where(x => x.TitleId == title.Id)
            .OrderBy(x => x.Number)
            .ToListAsync(ct);

        var episodeDtos = episodes.Select(x =>
        {
            var sources = x.Sources
                .Where(s => s.IsActive)
                .OrderBy(s => s.Priority)
                .Select(s => new PlaybackSource(s.Provider, s.Url, StreamFormat.Infer(s.Url, s.Format), s.Priority, s.SubtitleUrl, s.AudioTrack))
                .ToList();

            if (sources.Count == 0 && !string.IsNullOrWhiteSpace(x.HlsUrl))
            {
                sources.Add(new PlaybackSource(
                    "Primary",
                    x.HlsUrl,
                    StreamFormat.Infer(x.HlsUrl),
                    1,
                    x.SubtitleUrl));
            }

            var milestones = x.Milestones.HasIntro || x.Milestones.HasOutro
                ? new PlaybackMilestonesDto(x.Milestones.IntroStart, x.Milestones.IntroEnd, x.Milestones.OutroStart, x.Milestones.OutroEnd)
                : null;

            var primaryHls = sources.FirstOrDefault(s => s.Format == StreamFormat.Hls)?.Url ?? x.HlsUrl;

            return new PlaybackEpisode(x.Number, x.Name, primaryHls, x.SubtitleUrl, sources, milestones);
        }).ToList();

        return new(title.Slug.Value, title.LocalizedTitle(locale), title.Type.IsSeries, episodeDtos);
    }

    public async Task<HomeResponse?> GetHomeAsync(string locale, CancellationToken ct)
    {
        var titles = await db.Titles
            .AsNoTracking()
            .OrderByDescending(x => x.Featured)
            .ThenByDescending(x => x.Year)
            .Take(80)
            .ToListAsync(ct);
        var natraSlug = TitleSlug.Parse("natra-2-ma-dong-nao-hai");
        var hero = titles.FirstOrDefault(x => x.Slug == natraSlug)
            ?? titles.FirstOrDefault(x => x.Featured);
        if (hero is null) return null;

        var heroSummary = Summary(hero, locale);
        if (hero.Slug == natraSlug) heroSummary = heroSummary with { PosterUrl = NatraHeroBannerUrl };
        return new(heroSummary, titles.Select(x => Summary(x, locale)).ToList());
    }

    public async Task<ScheduleResponse> GetScheduleAsync(DateOnly weekStart, string locale, CancellationToken ct)
    {
        var start = new DateTimeOffset(weekStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(7);
        var titles = await db.Titles
            .AsNoTracking()
            .Where(x => x.Type == TitleType.Series && x.UpdatedAt >= start && x.UpdatedAt < end)
            .OrderBy(x => x.UpdatedAt)
            .ToListAsync(ct);
        var titleIds = titles.Select(x => x.Id).ToList();
        var episodes = await db.Episodes
            .AsNoTracking()

            .Where(x => titleIds.Contains(x.TitleId))
            .Select(x => new { x.TitleId, x.Number })
            .ToListAsync(ct);
        var latestEpisodes = episodes
            .GroupBy(x => x.TitleId)
            .ToDictionary(group => group.Key, group => (int?)group.Max(x => x.Number));
        var items = titles.Select(x => new ScheduleEntry(
            x.Slug.Value,
            x.LocalizedTitle(locale),
            x.PosterUrl,
            DateOnly.FromDateTime(x.UpdatedAt.UtcDateTime),
            latestEpisodes.GetValueOrDefault(x.Id))).ToList();

        return new ScheduleResponse(weekStart, items);
    }
    public async Task<PeopleResponse> ListPeopleAsync(string? query, int page, int pageSize, CancellationToken ct)
    {
        var titles = await db.Titles.AsNoTracking().ToListAsync(ct);
        var people = BuildPeople(titles);
        var normalizedQuery = query?.Trim();
        var filtered = people
            .Where(pair => string.IsNullOrWhiteSpace(normalizedQuery) ||
                pair.Key.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(pair => pair.Value.Titles.Count)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pair => new PersonSummary(
                PersonSlug(pair.Key),
                pair.Key,
                pair.Value.Roles.Order(StringComparer.OrdinalIgnoreCase).ToList(),
                pair.Value.Titles.Count))
            .ToList();
        return new PeopleResponse(items, filtered.Count);
    }

    public async Task<PersonDetail?> GetPersonAsync(string slug, string locale, CancellationToken ct)
    {
        var titles = await db.Titles.AsNoTracking().ToListAsync(ct);
        var match = BuildPeople(titles)
            .FirstOrDefault(pair => string.Equals(PersonSlug(pair.Key), slug, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(match.Key)) return null;
        return new PersonDetail(
            PersonSlug(match.Key),
            match.Key,
            match.Value.Roles.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            match.Value.Titles
                .OrderByDescending(title => title.Year)
                .Select(title => Summary(title, locale))
                .ToList());
    }

    private static Dictionary<string, (HashSet<string> Roles, List<Title> Titles)> BuildPeople(IReadOnlyList<Title> titles)
    {
        var people = new Dictionary<string, (HashSet<string> Roles, List<Title> Titles)>(StringComparer.OrdinalIgnoreCase);
        foreach (var title in titles)
        {
            AddCredits(title.Actors, "Diễn viên", title);
            AddCredits(title.Directors, "Đạo diễn", title);
        }

        return people;

        void AddCredits(string credits, string role, Title title)
        {
            foreach (var name in credits.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!people.TryGetValue(name, out var person))
                {
                    person = (new HashSet<string>(StringComparer.OrdinalIgnoreCase), []);
                    people.Add(name, person);
                }
                person.Roles.Add(role);
                if (!person.Titles.Contains(title)) person.Titles.Add(title);
            }
        }
    }

    private static string PersonSlug(string name)
    {
        var builder = new StringBuilder();
        var separatorPending = false;
        foreach (var character in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character))
            {
                if (separatorPending && builder.Length > 0) builder.Append('-');
                builder.Append(char.ToLowerInvariant(character));
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }
        return builder.ToString();
    }

    private static TitleSummary Summary(Title x, string locale) => new(x.Slug.Value, x.LocalizedTitle(locale), x.Genre, x.Year.Value, x.Type.Value, x.PosterUrl, x.IsR2Hosted, x.Country);
    private static TitleDetail Detail(Title x, string locale, long viewCount) => new(x.Slug.Value, x.LocalizedTitle(locale), x.LocalizedSynopsis(locale), x.Genre, x.Year.Value, x.Type.Value, x.PosterUrl, x.Runtime.Minutes, viewCount, x.Actors, x.Directors, x.Country, x.TrailerUrl, x.IsR2Hosted);
}
