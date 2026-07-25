using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class CatalogLibraryReader(CatalogDbContext db) : ILibraryCatalogReader
{
    public Task<Guid?> FindTitleIdAsync(string slug, CancellationToken ct) =>
        db.Titles.AsNoTracking().Where(x => x.Slug == slug).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);

    public async Task<PlayableReference?> FindPlayableAsync(string slug, int? episodeNumber, CancellationToken ct)
    {
        var title = await db.Titles.AsNoTracking().Where(x => x.Slug == slug).Select(x => new { x.Id, x.Type }).FirstOrDefaultAsync(ct);
        if (title is null) return null;
        if (episodeNumber is null) return new PlayableReference(title.Id, title.Id, null);
        var episodeId = await db.Episodes.AsNoTracking().Where(x => x.TitleId == title.Id && x.Number == episodeNumber).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        return episodeId is null ? null : new PlayableReference(title.Id, episodeId.Value, episodeNumber);
    }

    public async Task<IReadOnlyDictionary<Guid, LibraryTitle>> GetTitlesAsync(IEnumerable<Guid> titleIds, string locale, CancellationToken ct)
    {
        var ids = titleIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, LibraryTitle>();
        var titles = await db.Titles.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        return titles.ToDictionary(x => x.Id, x => new LibraryTitle(x.Slug, x.LocalizedTitle(locale), x.Genre, x.Year, x.Type, x.PosterUrl, x.RuntimeMinutes));
    }

    public async Task<IReadOnlyList<LibraryTitle>> GetDiscoveryTitlesAsync(string locale, CancellationToken ct) =>
        (await db.Titles.AsNoTracking().OrderByDescending(x => x.Featured).ThenByDescending(x => x.Year).ToListAsync(ct))
            .Select(x => new LibraryTitle(x.Slug, x.LocalizedTitle(locale), x.Genre, x.Year, x.Type, x.PosterUrl, x.RuntimeMinutes)).ToList();

    public async Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationCandidatesAsync(string locale, CancellationToken ct) =>
        // Keep assistant retrieval bounded. The recommender only returns a handful of
        // suggestions, so loading the entire production catalog is unnecessary and can
        // hold a database request open long enough for the edge proxy to return 524.
        (await db.Titles.AsNoTracking()
            .OrderByDescending(x => x.Featured)
            .ThenByDescending(x => x.Year)
            .Take(500)
            .ToListAsync(ct))
            .Select(x => new RecommendationCandidate(x.Id, new LibraryTitle(x.Slug, x.LocalizedTitle(locale), x.Genre, x.Year, x.Type, x.PosterUrl, x.RuntimeMinutes), x.LocalizedSynopsis(locale))).ToList();
}
