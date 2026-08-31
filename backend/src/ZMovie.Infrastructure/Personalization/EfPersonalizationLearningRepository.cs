using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Personalization;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Infrastructure.Personalization;

public sealed class EfPersonalizationLearningRepository(PersonalizationDbContext db) : IPersonalizationLearningRepository
{
    public void Add(AssistantLearningEvent learningEvent) =>
        db.AssistantLearningEvents.Add(learningEvent);

    public void AddRange(IEnumerable<AssistantLearningEvent> learningEvents) =>
        db.AssistantLearningEvents.AddRange(learningEvents);

    public async Task<AssistantLearningEvent?> FindLatestImpressionAsync(
        UserId userId,
        RecommendationId recommendationId,
        TitleId titleId,
        CancellationToken ct)
    {
        return await db.AssistantLearningEvents
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RecommendationId == recommendationId && x.TitleId == titleId && x.EventType == FeedbackEventType.ImpressionName)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct) =>
        await db.SaveChangesAsync(ct);
}
