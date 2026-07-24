using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfUserLibraryStore(CatalogDbContext db) : IUserLibraryStore, IViewAnalyticsStore, ITitleReviewStore
{
    public async Task<IReadOnlyList<SavedTitleEntry>> GetSavedAsync(Guid userId, CancellationToken ct) =>
        await db.SavedTitles.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.SavedAt)
            .Select(x => new SavedTitleEntry(x.TitleId, x.SavedAt)).ToListAsync(ct);

    public async Task<IReadOnlyList<WatchProgressEntry>> GetHistoryAsync(Guid userId, CancellationToken ct)
    {
        var items = await db.WatchHistory.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.UpdatedAt)
            .Select(x => new WatchProgressEntry(x.TitleId, x.PlayableId, x.EpisodeNumber, x.ProgressSeconds, x.UpdatedAt)).ToListAsync(ct);
        return items.GroupBy(x => x.TitleId).Select(x => x.First()).ToList();
    }

    public async Task SaveAsync(Guid userId, Guid titleId, CancellationToken ct)
    {
        if (await db.SavedTitles.AnyAsync(x => x.UserId == userId && x.TitleId == titleId, ct)) return;
        db.SavedTitles.Add(new SavedTitle { UserId = userId, TitleId = titleId });
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid titleId, CancellationToken ct)
    {
        var saved = await db.SavedTitles.FirstOrDefaultAsync(x => x.UserId == userId && x.TitleId == titleId, ct);
        if (saved is null) return false;
        db.SavedTitles.Remove(saved);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task RecordProgressAsync(Guid userId, PlayableReference playable, double progressSeconds, CancellationToken ct)
    {
        var item = await db.WatchHistory.FirstOrDefaultAsync(x => x.UserId == userId && x.PlayableId == playable.PlayableId, ct);
        if (item is null) db.WatchHistory.Add(new WatchProgress { UserId = userId, PlayableId = playable.PlayableId, TitleId = playable.TitleId, EpisodeNumber = playable.EpisodeNumber, ProgressSeconds = Math.Max(0, progressSeconds) });
        else { item.EpisodeNumber = playable.EpisodeNumber; item.ProgressSeconds = Math.Max(0, progressSeconds); item.UpdatedAt = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(ct);
    }

    public async Task<ViewRecordedResponse> RecordAsync(Guid titleId, Guid? userId, string sessionId, int? episodeNumber, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var identity = userId?.ToString("N") ?? sessionId;
        var lockKey = $"view:{titleId:N}:{episodeNumber?.ToString() ?? "title"}:{(userId is null ? "session" : "user")}:{identity}";
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) await AcquirePostgresLockAsync(db, lockKey, ct);

        var dedupeAfter = now.AddMinutes(-30);
        var alreadyCounted = await HasRecentEventAsync(db, titleId, episodeNumber, userId, sessionId, dedupeAfter, ct);
        if (!alreadyCounted)
        {
            db.TitleViewEvents.Add(new TitleViewEvent { TitleId = titleId, UserId = userId, SessionId = sessionId, EpisodeNumber = episodeNumber, ViewedAt = now });
            await db.SaveChangesAsync(ct);
        }

        var count = await db.TitleViewEvents.LongCountAsync(x => x.TitleId == titleId, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return new ViewRecordedResponse(count, !alreadyCounted);
    }

    public Task<long> GetViewCountAsync(Guid titleId, CancellationToken ct) =>
        db.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.TitleId == titleId, ct);

    public async Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, CancellationToken ct)
    {
        var start = PeriodStart(period, DateTimeOffset.UtcNow);
        return await db.TitleViewEvents.AsNoTracking().Where(x => x.ViewedAt >= start)
            .GroupBy(x => x.TitleId).Select(x => new { x.Key, Views = x.LongCount() })
            .OrderByDescending(x => x.Views).ThenBy(x => x.Key).Take(limit)
            .Select(x => new TopViewCount(x.Key, x.Views)).ToListAsync(ct);
    }

    private static DateTimeOffset PeriodStart(TopPeriod period, DateTimeOffset now)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var localNow = TimeZoneInfo.ConvertTime(now, zone);
        var date = DateOnly.FromDateTime(localNow.Date);
        var startDate = period switch
        {
            TopPeriod.Day => date,
            TopPeriod.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            TopPeriod.Month => new DateOnly(date.Year, date.Month, 1),
            _ => date,
        };
        var localStart = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(localStart, zone.GetUtcOffset(localStart)).ToUniversalTime();
    }

    public async Task<IReadOnlyList<ReviewEntry>> GetAsync(Guid titleId, CancellationToken ct) =>
        await db.TitleReviews.AsNoTracking().Where(x => x.TitleId == titleId).OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ReviewEntry(x.Id, x.AuthorName, x.Rating, x.Comment, x.UpdatedAt)).ToListAsync(ct);

    public async Task UpsertAsync(Guid titleId, Guid userId, string authorName, int rating, string? comment, CancellationToken ct)
    {
        var review = await db.TitleReviews.SingleOrDefaultAsync(x => x.TitleId == titleId && x.UserId == userId, ct);
        if (review is null) db.TitleReviews.Add(new TitleReview { TitleId = titleId, UserId = userId, AuthorName = authorName, Rating = rating, Comment = string.IsNullOrWhiteSpace(comment) ? null : comment });
        else { review.AuthorName = authorName; review.Rating = rating; review.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment; review.UpdatedAt = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveReviewAsync(Guid titleId, Guid userId, CancellationToken ct)
    {
        var review = await db.TitleReviews.SingleOrDefaultAsync(x => x.TitleId == titleId && x.UserId == userId, ct);
        if (review is null) return false;
        db.TitleReviews.Remove(review);
        await db.SaveChangesAsync(ct);
        return true;
    }

    [ExcludeFromCodeCoverage]
    private static Task AcquirePostgresLockAsync(CatalogDbContext db, string lockKey, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);

    [ExcludeFromCodeCoverage]
    private static async Task<bool> HasRecentEventAsync(CatalogDbContext db, Guid titleId, int? episodeNumber, Guid? userId, string sessionId, DateTimeOffset dedupeAfter, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql())
        {
            var localEvents = await db.TitleViewEvents.Where(x => x.TitleId == titleId && x.EpisodeNumber == episodeNumber).ToListAsync(ct);
            return userId is { } localId
                ? localEvents.Any(x => x.UserId == localId && x.ViewedAt >= dedupeAfter)
                : localEvents.Any(x => x.UserId == null && x.SessionId == sessionId && x.ViewedAt >= dedupeAfter);
        }

        var events = db.TitleViewEvents.Where(x => x.TitleId == titleId && x.EpisodeNumber == episodeNumber && x.ViewedAt >= dedupeAfter);
        return userId is { } id
            ? await events.AnyAsync(x => x.UserId == id, ct)
            : await events.AnyAsync(x => x.UserId == null && x.SessionId == sessionId, ct);
    }
}
