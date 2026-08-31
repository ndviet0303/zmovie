using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Personalization;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Infrastructure.Personalization;

public sealed class EfPersonalizationTitleCleanupPort(PersonalizationDbContext db) : IPersonalizationTitleCleanupPort
{
    private const int DeleteBatchSize = 500;

    public async Task DeleteByTitleIdAsync(TitleId titleId, CancellationToken ct)
    {
        await DeleteInBatchesAsync(db.AssistantLearningEvents.Where(x => x.TitleId == titleId), db, ct);
    }

    private static async Task DeleteInBatchesAsync<TEntity>(IQueryable<TEntity> source, DbContext context, CancellationToken ct) where TEntity : class
    {
        while (true)
        {
            var batch = await source.Take(DeleteBatchSize).ToListAsync(ct);
            if (batch.Count == 0) return;

            context.Set<TEntity>().RemoveRange(batch);
            await context.SaveChangesAsync(ct);
            if (batch.Count < DeleteBatchSize) return;
        }
    }
}
