using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZMovie.Application.Assistant;
using ZMovie.Infrastructure.Persistence;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Assistant;

public sealed class EfAssistantLearningStore(CatalogDbContext db, ILogger<EfAssistantLearningStore> logger) : IAssistantLearningStore
{
    public async Task<Guid?> RecordImpressionAsync(Guid userId, string message, IReadOnlyList<AssistantCatalogTitle> suggestions, CancellationToken ct)
    {
        if (suggestions.Count == 0) return null;

        var features = string.Join(',', AssistantMood.SearchTermWeights(message).Keys.Take(24).Select(HashFeature));
        if (features.Length == 0) return null;
        var recommendationId = Guid.CreateVersion7();
        try
        {
            var slugs = suggestions.Select(x => x.Title.Slug).ToList();
            var titleIds = await db.Titles.AsNoTracking().Where(x => slugs.Contains(x.Slug))
                .ToDictionaryAsync(x => x.Slug, x => x.Id, ct);
            var events = suggestions.Where(x => titleIds.ContainsKey(x.Title.Slug)).Select((suggestion, index) => new AssistantLearningEvent
            {
                RecommendationId = recommendationId,
                UserId = userId,
                TitleId = titleIds[suggestion.Title.Slug],
                Features = features,
                Rank = index + 1,
                EventType = "impression",
                Reward = 0,
            }).ToList();
            if (events.Count == 0) return null;
            db.AssistantLearningEvents.AddRange(events);
            await db.SaveChangesAsync(ct);
            return recommendationId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Assistant impression logging is unavailable; continuing without learning telemetry.");
            return null;
        }
    }

    public async Task<bool> RecordFeedbackAsync(Guid userId, Guid recommendationId, Guid titleId, string eventType, CancellationToken ct)
    {
        if (!AssistantFeedbackEvents.TryGetReward(eventType, out var reward)) return false;
        try
        {
            var impression = await db.AssistantLearningEvents.AsNoTracking()
                .Where(x => x.UserId == userId && x.RecommendationId == recommendationId && x.TitleId == titleId && x.EventType == "impression")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (impression is null) return false;

            db.AssistantLearningEvents.Add(new AssistantLearningEvent
            {
                RecommendationId = recommendationId,
                UserId = userId,
                TitleId = titleId,
                Features = impression.Features,
                Rank = impression.Rank,
                EventType = eventType.Trim().ToLowerInvariant(),
                Reward = reward,
            });
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Assistant feedback logging is unavailable; continuing without learning telemetry.");
            return false;
        }
    }

    public async Task<IReadOnlyDictionary<Guid, double>> GetTitleScoresAsync(Guid userId, IReadOnlyDictionary<string, int> tokens, CancellationToken ct)
    {
        if (tokens.Count == 0) return new Dictionary<Guid, double>();
        try
        {
            var hashedTokens = tokens.ToDictionary(x => HashFeature(x.Key), x => x.Value, StringComparer.Ordinal);
            var since = DateTimeOffset.UtcNow.AddDays(-180);
            var events = await db.AssistantLearningEvents.AsNoTracking()
                .Where(x => x.UserId == userId && x.EventType != "impression" && x.CreatedAt >= since)
                .OrderByDescending(x => x.CreatedAt)
                .Take(1_000)
                .ToListAsync(ct);
            return events.GroupBy(x => x.TitleId).ToDictionary(x => x.Key, x => x.Sum(eventItem =>
            {
                var ageDays = Math.Max(0, (DateTimeOffset.UtcNow - eventItem.CreatedAt).TotalDays);
                var decay = Math.Exp(-ageDays / 45d);
                var matchWeight = eventItem.Features.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Sum(feature => hashedTokens.TryGetValue(feature, out var weight) ? weight : 0);
                return eventItem.Reward * matchWeight * decay;
            }));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Assistant learning scores are unavailable; using the base ranker.");
            return new Dictionary<Guid, double>();
        }
    }

    private static string HashFeature(string feature) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(feature))).ToLowerInvariant();

}
