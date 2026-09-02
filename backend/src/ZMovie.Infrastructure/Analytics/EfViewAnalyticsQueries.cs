using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Analytics;
using ZMovie.Domain.Analytics;
using ZMovie.Infrastructure.Analytics.Persistence;

namespace ZMovie.Infrastructure.Analytics;

public sealed class EfViewAnalyticsQueries(AnalyticsDbContext db) : IViewAnalyticsQueries
{
    public Task<long> GetViewCountAsync(TitleId titleId, CancellationToken ct) =>
        db.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.TitleId == titleId, ct);

    public async Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, DateTimeOffset now, CancellationToken ct)
    {
        var start = PeriodCalculator.CalculatePeriodStart(period, now);
        return await db.TitleViewEvents.AsNoTracking()
            .Where(x => x.ViewedAt >= start)
            .GroupBy(x => x.TitleId)
            .Select(x => new { x.Key, Views = x.LongCount() })
            .OrderByDescending(x => x.Views)
            .ThenBy(x => x.Key)
            .Take(limit)
            .Select(x => new TopViewCount(x.Key.Value, x.Views))
            .ToListAsync(ct);
    }
}
