using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfWatchProgressRepository(EngagementDbContext db) : IWatchProgressRepository
{
    private DbSet<WatchProgress> WatchHistory => db.WatchHistory;

    public Task<WatchProgress?> FindAsync(UserId userId, PlayableId playableId, CancellationToken ct) =>
        WatchHistory.SingleOrDefaultAsync(progress => progress.UserId == userId && progress.PlayableId == playableId, ct);

    public void Add(WatchProgress progress) => WatchHistory.Add(progress);

    public async Task RemoveByTitleAsync(UserId userId, TitleId titleId, CancellationToken ct)
    {
        var items = await WatchHistory.Where(x => x.UserId == userId && x.TitleId == titleId).ToListAsync(ct);
        WatchHistory.RemoveRange(items);
    }

    public async Task ClearAllAsync(UserId userId, CancellationToken ct)
    {
        var items = await WatchHistory.Where(x => x.UserId == userId).ToListAsync(ct);
        WatchHistory.RemoveRange(items);
    }

    public async Task SaveChangesAsync(CancellationToken ct) =>
        await db.SaveChangesAsync(ct);
}
