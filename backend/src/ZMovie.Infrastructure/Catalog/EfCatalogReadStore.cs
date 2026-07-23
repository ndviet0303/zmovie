using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfCatalogReadStore(CatalogDbContext db, IViewAnalyticsStore analytics) : ICatalogReadStore
{
    private const string NatraHeroBannerUrl = "https://cdnstatic.usheru.com/img/movies/original_8btfz81bOJ2lC7cujYBTw03wzg3.jpg";

    public async Task<TitleListResponse> ListAsync(string? query, string? genre, string locale, CancellationToken ct)
    {
        var titles = db.Titles.AsNoTracking().AsQueryable();
        var q = query?.Trim();
        if (!string.IsNullOrWhiteSpace(q)) titles = titles.Where(x => x.EnglishTitle.Contains(q) || x.VietnameseTitle.Contains(q) || x.Genre.Contains(q));
        if (!string.IsNullOrWhiteSpace(genre))
        {
            var selectedGenre = genre.Trim();
            titles = titles.Where(x => EF.Functions.ILike(x.Genre, $"%{selectedGenre}%"));
        }
        // Browse currently has no pagination UI; retain a bounded demo payload rather than
        // serializing the entire imported catalog on every request.
        var items = await titles.OrderByDescending(x => x.Featured).ThenByDescending(x => x.Year).Take(500).ToListAsync(ct);
        return new(items.Select(x => Summary(x, locale)).ToList(), items.Count);
    }

    public async Task<TitleDetail?> GetAsync(string slug, string locale, CancellationToken ct)
    {
        var title = await db.Titles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (title is null) return null;
        return Detail(title, locale, await analytics.GetViewCountAsync(title.Id, ct));
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct)
    {
        var imported = await db.Genres.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(ct);
        return imported.Count > 0 ? imported : await db.Titles.AsNoTracking().Select(x => x.Genre).Distinct().Order().ToListAsync(ct);
    }
    public async Task<PlaybackResponse?> GetPlaybackAsync(string slug, string locale, CancellationToken ct)
    {
        var title = await db.Titles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (title is null) return null;
        var episodes = await db.Episodes.AsNoTracking().Where(x => x.TitleId == title.Id).OrderBy(x => x.Number).Select(x => new PlaybackEpisode(x.Number, x.Name, x.HlsUrl)).ToListAsync(ct);
        return new(title.Slug, title.LocalizedTitle(locale), title.Type == "series", episodes);
    }
    public async Task<HomeResponse?> GetHomeAsync(string locale, CancellationToken ct)
    {
        var titles = await db.Titles
            .AsNoTracking()
            .OrderByDescending(x => x.Featured)
            .ThenByDescending(x => x.Year)
            .Take(80)
            .ToListAsync(ct);
        var hero = titles.FirstOrDefault(x => x.Slug == "natra-2-ma-dong-nao-hai")
            ?? titles.FirstOrDefault(x => x.Featured);
        if (hero is null) return null;

        var heroSummary = Summary(hero, locale);
        if (hero.Slug == "natra-2-ma-dong-nao-hai") heroSummary = heroSummary with { PosterUrl = NatraHeroBannerUrl };
        return new(heroSummary, titles.Select(x => Summary(x, locale)).ToList());
    }

    private static TitleSummary Summary(CatalogTitle x, string locale) => new(x.Slug, x.LocalizedTitle(locale), x.Genre, x.Year, x.Type, x.PosterUrl);
    private static TitleDetail Detail(CatalogTitle x, string locale, long viewCount) => new(x.Slug, x.LocalizedTitle(locale), x.LocalizedSynopsis(locale), x.Genre, x.Year, x.Type, x.PosterUrl, x.RuntimeMinutes, viewCount);
}
