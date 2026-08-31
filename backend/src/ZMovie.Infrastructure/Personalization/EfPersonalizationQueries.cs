using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZMovie.Application.Personalization;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Infrastructure.Personalization;

public sealed class EfPersonalizationQueries(PersonalizationDbContext db, ILogger<EfPersonalizationQueries> logger) : IPersonalizationQueries
{
    public async Task<IReadOnlyDictionary<Guid, double>> GetTitleScoresAsync(
        UserId userId,
        IReadOnlyDictionary<string, int> tokens,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (tokens.Count == 0) return new Dictionary<Guid, double>();

        try
        {
            var hashedTokens = tokens.ToDictionary(x => HashFeature(x.Key), x => x.Value, StringComparer.Ordinal);
            var since = now.AddDays(-180);
            var events = await db.AssistantLearningEvents.AsNoTracking()
                .Where(x => x.UserId == userId && x.EventType != FeedbackEventType.ImpressionName && x.CreatedAt >= since)
                .OrderByDescending(x => x.CreatedAt)
                .Take(1_000)
                .ToListAsync(ct);

            return events.GroupBy(x => x.TitleId.Value).ToDictionary(x => x.Key, x => x.Sum(eventItem =>
            {
                var ageDays = Math.Max(0, (now - eventItem.CreatedAt).TotalDays);
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

    public static string HashFeature(string feature) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(feature))).ToLowerInvariant();
}
