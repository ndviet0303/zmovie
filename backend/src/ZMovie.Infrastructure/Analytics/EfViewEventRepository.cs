using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Analytics;
using ZMovie.Domain.Analytics;
using ZMovie.Infrastructure.Analytics.Persistence;

namespace ZMovie.Infrastructure.Analytics;

public sealed class EfViewEventRepository(AnalyticsDbContext db) : IViewEventRepository
{
    public async Task<ViewRecordedResponse> RecordViewWithLockAsync(
        TitleId titleId,
        int? episodeNumber,
        UserId? userId,
        string sessionId,
        DateTimeOffset occurredAt,
        CancellationToken ct)
    {
        var identity = userId.HasValue ? userId.Value.Value.ToString("N") : sessionId;
        var lockKey = $"view:{titleId.Value:N}:{episodeNumber?.ToString() ?? "title"}:{(userId is null ? "session" : "user")}:{identity}";

        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (transaction is not null)
        {
            await AcquirePostgresLockAsync(db, lockKey, ct);
        }

        var dedupeAfter = DeduplicationPolicy.DeduplicationThreshold(occurredAt);
        var alreadyCounted = await HasRecentEventAsync(db, titleId, episodeNumber, userId, sessionId, dedupeAfter, ct);

        if (!alreadyCounted)
        {
            var viewEvent = TitleViewEvent.Record(
                ViewEventId.New(),
                titleId,
                episodeNumber,
                userId,
                sessionId,
                occurredAt);

            db.TitleViewEvents.Add(viewEvent);
            await db.SaveChangesAsync(ct);
        }

        var totalViews = await db.TitleViewEvents.LongCountAsync(x => x.TitleId == titleId, ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        return new ViewRecordedResponse(totalViews, !alreadyCounted);
    }

    public Task<long> GetViewCountAsync(TitleId titleId, CancellationToken ct) =>
        db.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.TitleId == titleId, ct);

    [ExcludeFromCodeCoverage]
    private static Task AcquirePostgresLockAsync(AnalyticsDbContext db, string lockKey, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);

    [ExcludeFromCodeCoverage]
    private static async Task<bool> HasRecentEventAsync(
        AnalyticsDbContext db,
        TitleId titleId,
        int? episodeNumber,
        UserId? userId,
        string sessionId,
        DateTimeOffset dedupeAfter,
        CancellationToken ct)
    {
        if (!db.Database.IsNpgsql())
        {
            var localEvents = await db.TitleViewEvents
                .Where(x => x.TitleId == titleId && x.EpisodeNumber == episodeNumber)
                .ToListAsync(ct);

            return userId.HasValue
                ? localEvents.Any(x => x.UserId == userId && x.ViewedAt >= dedupeAfter)
                : localEvents.Any(x => x.UserId == null && x.SessionId == sessionId && x.ViewedAt >= dedupeAfter);
        }

        var events = db.TitleViewEvents.Where(x => x.TitleId == titleId && x.EpisodeNumber == episodeNumber && x.ViewedAt >= dedupeAfter);
        return userId.HasValue
            ? await events.AnyAsync(x => x.UserId == userId, ct)
            : await events.AnyAsync(x => x.UserId == null && x.SessionId == sessionId, ct);
    }
}
