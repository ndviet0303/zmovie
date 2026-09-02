using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;
using ZMovie.Application.Personalization;
using EngagementUserId = ZMovie.Domain.Engagement.UserId;
using PersonalizationUserId = ZMovie.Domain.Personalization.UserId;

namespace ZMovie.Infrastructure.Assistant;

public sealed class CatalogAssistantStore(
    ILibraryCatalogReader catalog,
    IUserLibraryQueries library,
    IRecommendationEngine recommender,
    IPersonalizationQueries? personalization = null,
    TimeProvider? timeProvider = null) : ICatalogAssistantStore
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<IReadOnlyList<AssistantCatalogTitle>> SearchAsync(Guid userId, string message, string locale, int limit, CancellationToken ct)
    {
        var tokens = AssistantMood.SearchTermWeights(message);
        if (tokens.Count == 0) return [];

        var saved = await library.ListSavedAsync(new EngagementUserId(userId), ct);
        var history = await library.ListHistoryAsync(new EngagementUserId(userId), ct);
        var candidates = await catalog.GetRecommendationCandidatesAsync(locale, ct);

        var profile = saved.Select(x => new RecommendationSeed(x.TitleId, 1))
            .Concat(history.Select(x => new RecommendationSeed(x.TitleId, 3))).ToList();
        var excluded = saved.Select(x => x.TitleId).Concat(history.Select(x => x.TitleId)).ToHashSet();

        var personalizedIds = profile.Count == 0
            ? []
            : recommender.Recommend(candidates, profile, excluded, Math.Max(limit * 3, 12)).ToHashSet();

        var now = _timeProvider.GetUtcNow();
        var learnedScores = personalization is null
            ? new Dictionary<Guid, double>()
            : await personalization.GetTitleScoresAsync(new PersonalizationUserId(userId), tokens, now, ct);

        return candidates.Select(candidate => new
        {
            Item = ToAssistantTitle(candidate),
            Score = Score(candidate, tokens) + (personalizedIds.Contains(candidate.TitleId) ? 4 : 0)
                + Math.Clamp(learnedScores.GetValueOrDefault(candidate.TitleId), -6, 6),
        })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.Title.Year)
            .Take(limit)
            .Select(x => x.Item)
            .ToList();
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
