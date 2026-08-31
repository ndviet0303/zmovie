using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfEngagementTitleCleanupPort(EngagementDbContext db) : IEngagementTitleCleanupPort
{
    private const int DeleteBatchSize = 500;

    public async Task DeleteByTitleIdAsync(TitleId titleId, CancellationToken ct)
    {
        await DeleteInBatchesAsync(db.WatchHistory.Where(x => x.TitleId == titleId), db, ct);
        await DeleteInBatchesAsync(db.SavedTitles.Where(x => x.TitleId == titleId), db, ct);
        await DeleteInBatchesAsync(db.TitleReviews.Where(x => x.TitleId == titleId), db, ct);
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
