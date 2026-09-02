using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfDanmakuRepository(EngagementDbContext db) : IDanmakuRepository
{
    public async Task<IReadOnlyList<DanmakuComment>> GetTimedCommentsAsync(
        string titleSlug,
        int episodeNumber,
        int? fromSeconds,
        int? toSeconds,
        CancellationToken ct)
    {
        var query = db.DanmakuComments
            .AsNoTracking()
            .Where(x => x.TitleSlug == titleSlug && x.EpisodeNumber == episodeNumber);

        if (fromSeconds.HasValue)
        {
            query = query.Where(x => x.TimeSeconds >= fromSeconds.Value);
        }

        if (toSeconds.HasValue)
        {
            query = query.Where(x => x.TimeSeconds <= toSeconds.Value);
        }

        return await query.OrderBy(x => x.TimeSeconds).ToListAsync(ct);
    }

    public async Task AddAsync(DanmakuComment comment, CancellationToken ct)
    {
        await db.DanmakuComments.AddAsync(comment, ct);
        await db.SaveChangesAsync(ct);
    }
}
