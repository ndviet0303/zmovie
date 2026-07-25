using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Assistant;

public sealed class CatalogAssistantStore : ICatalogAssistantStore
{
    private readonly CatalogDbContext _db;
    private readonly IUserLibraryStore? _library;
    private readonly ILibraryCatalogReader? _catalog;
    private readonly IRecommendationEngine? _recommender;

    public CatalogAssistantStore(CatalogDbContext db) => _db = db;

    public CatalogAssistantStore(CatalogDbContext db, IUserLibraryStore library, ILibraryCatalogReader catalog, IRecommendationEngine recommender)
    {
        _db = db;
        _library = library;
        _catalog = catalog;
        _recommender = recommender;
    }

    public async Task<IReadOnlyList<AssistantCatalogTitle>> SearchAsync(Guid userId, string message, string locale, int limit, CancellationToken ct)
    {
        var tokens = AssistantMood.SearchTermWeights(message);
        if (tokens.Count == 0) return [];

        if (_library is null || _catalog is null || _recommender is null)
            return await SearchCatalogAsync(tokens, locale, limit, ct);

        var saved = await _library.GetSavedAsync(userId, ct);
        var history = await _library.GetHistoryAsync(userId, ct);
        var candidates = await _catalog.GetRecommendationCandidatesAsync(locale, ct);
        var profile = saved.Select(x => new RecommendationSeed(x.TitleId, 1))
            .Concat(history.Select(x => new RecommendationSeed(x.TitleId, 3))).ToList();
        var excluded = saved.Select(x => x.TitleId).Concat(history.Select(x => x.TitleId)).ToHashSet();
        var personalizedIds = profile.Count == 0
            ? []
            : _recommender.Recommend(candidates, profile, excluded, Math.Max(limit * 3, 12)).ToHashSet();

        return candidates.Select(candidate => new
        {
            Item = ToAssistantTitle(candidate),
            Score = Score(candidate, tokens) + (personalizedIds.Contains(candidate.TitleId) ? 4 : 0),
        })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Item.Title.Year).Take(limit).Select(x => x.Item).ToList();
    }

    private async Task<IReadOnlyList<AssistantCatalogTitle>> SearchCatalogAsync(IReadOnlyDictionary<string, int> tokens, string locale, int limit, CancellationToken ct)
    {
        var titles = await _db.Titles.AsNoTracking().ToListAsync(ct);
        return titles.Select(title => new
        {
            Item = new AssistantCatalogTitle(new TitleSummary(title.Slug, title.LocalizedTitle(locale), title.Genre, title.Year, title.Type, title.PosterUrl), title.LocalizedSynopsis(locale)),
            Score = Score(title.LocalizedTitle(locale), title.Genre, title.LocalizedSynopsis(locale), tokens),
        })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Item.Title.Year).Take(limit).Select(x => x.Item).ToList();
    }

    private static AssistantCatalogTitle ToAssistantTitle(RecommendationCandidate candidate) =>
        new(new TitleSummary(candidate.Title.Slug, candidate.Title.Title, candidate.Title.Genre, candidate.Title.Year, candidate.Title.Type, candidate.Title.PosterUrl), candidate.Synopsis);

    private static int Score(RecommendationCandidate candidate, IReadOnlyDictionary<string, int> tokens) =>
        Score(candidate.Title.Title, candidate.Title.Genre, candidate.Synopsis, tokens);

    private static int Score(string title, string genre, string synopsis, IReadOnlyDictionary<string, int> tokens)
    {
        var name = title.ToLowerInvariant();
        var text = $"{name} {genre} {synopsis}".ToLowerInvariant();
        return tokens.Sum(token => token.Value * ((name.Contains(token.Key) ? 5 : 0) + (genre.Contains(token.Key, StringComparison.OrdinalIgnoreCase) ? 3 : 0) + (text.Contains(token.Key) ? 1 : 0)));
    }
}
