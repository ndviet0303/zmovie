using Microsoft.Extensions.Logging;
using ZMovie.Application.Assistant;
using ZMovie.Application.Engagement;
using ZMovie.Application.Personalization;
using ZMovie.Domain.Personalization;

namespace ZMovie.Infrastructure.Personalization;

public sealed class AssistantImpressionRecorder(
    IPersonalizationLearningRepository repository,
    ILibraryCatalogReader catalog,
    TimeProvider timeProvider,
    ILogger<AssistantImpressionRecorder> logger) : IAssistantImpressionRecorder
{
    public async Task<Guid?> RecordImpressionAsync(
        Guid userId,
        string message,
        IReadOnlyList<string> titleSlugs,
        CancellationToken ct)
    {
        if (titleSlugs.Count == 0) return null;

        var features = string.Join(',', AssistantMood.SearchTermWeights(message).Keys.Take(24).Select(EfPersonalizationQueries.HashFeature));
        if (features.Length == 0) return null;

        var recId = RecommendationId.New();
        var userTypedId = new UserId(userId);
        var now = timeProvider.GetUtcNow();

        try
        {
            var events = new List<AssistantLearningEvent>();
            for (var i = 0; i < titleSlugs.Count; i++)
            {
                var slug = titleSlugs[i];
                var titleIdGuid = await catalog.FindTitleIdAsync(slug, ct);
                if (titleIdGuid is null) continue;

                var evt = AssistantLearningEvent.RecordImpression(
                    LearningEventId.New(),
                    recId,
                    userTypedId,
                    new TitleId(titleIdGuid.Value),
                    features,
                    i + 1,
                    now);

                events.Add(evt);
            }

            if (events.Count == 0) return null;

            repository.AddRange(events);
            await repository.SaveChangesAsync(ct);
            return recId.Value;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Assistant impression logging is unavailable; continuing without learning telemetry.");
            return null;
        }
    }
}
