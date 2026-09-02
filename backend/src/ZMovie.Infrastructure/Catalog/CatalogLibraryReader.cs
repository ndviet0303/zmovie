using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class CatalogLibraryReader(CatalogDbContext db) : ILibraryCatalogReader
{
    public async Task<Guid?> FindTitleIdAsync(string slug, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        return await db.Titles.AsNoTracking()
            .Where(x => x.Slug == titleSlug)
            .Select(x => (Guid?)x.Id.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PlayableReference?> FindPlayableAsync(string slug, int? episodeNumber, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        var title = await db.Titles.AsNoTracking()
            .Where(x => x.Slug == titleSlug)
            .Select(x => new { x.Id, x.Type })
            .FirstOrDefaultAsync(ct);

        if (title is null) return null;
        if (episodeNumber is null) return new PlayableReference(title.Id.Value, title.Id.Value, null);

        var episodeId = await db.Episodes.AsNoTracking()
            .Where(x => x.TitleId == title.Id && x.Number == episodeNumber)
            .Select(x => (Guid?)x.Id.Value)
            .FirstOrDefaultAsync(ct);

        return episodeId is null ? null : new PlayableReference(title.Id.Value, episodeId.Value, episodeNumber);
    }

    public async Task<IReadOnlyDictionary<Guid, LibraryTitle>> GetTitlesAsync(IEnumerable<Guid> titleIds, string locale, CancellationToken ct)
    {
        var typedIds = titleIds.Distinct().Select(id => new TitleId(id)).ToArray();
        if (typedIds.Length == 0) return new Dictionary<Guid, LibraryTitle>();

        var titles = await db.Titles.AsNoTracking().Where(x => typedIds.Contains(x.Id)).ToListAsync(ct);
        return titles.ToDictionary(
            x => x.Id.Value,
            x => new LibraryTitle(x.Slug.Value, x.LocalizedTitle(locale), x.Genre, x.Year.Value, x.Type.Value, x.PosterUrl, x.Runtime.Minutes));
    }

    public async Task<IReadOnlyList<LibraryTitle>> GetDiscoveryTitlesAsync(string locale, CancellationToken ct) =>
        (await db.Titles.AsNoTracking().OrderByDescending(x => x.Featured).ThenByDescending(x => x.Year).ToListAsync(ct))
            .Select(x => new LibraryTitle(x.Slug.Value, x.LocalizedTitle(locale), x.Genre, x.Year.Value, x.Type.Value, x.PosterUrl, x.Runtime.Minutes))
            .ToList();

    public async Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationCandidatesAsync(string locale, CancellationToken ct) =>
        // Keep assistant retrieval bounded. The recommender only returns a handful of
        // suggestions, so loading the entire production catalog is unnecessary and can
        // hold a database request open long enough for the edge proxy to return 524.
        (await db.Titles.AsNoTracking()
            .OrderByDescending(x => x.Featured)
            .ThenByDescending(x => x.Year)
            .Take(500)
            .ToListAsync(ct))
            .Select(x => new RecommendationCandidate(
                x.Id.Value,
                new LibraryTitle(x.Slug.Value, x.LocalizedTitle(locale), x.Genre, x.Year.Value, x.Type.Value, x.PosterUrl, x.Runtime.Minutes),
                x.LocalizedSynopsis(locale)))
            .ToList();
}
