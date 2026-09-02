using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfUserLibraryQueries(EngagementDbContext db) : IUserLibraryQueries
{
    public async Task<IReadOnlyList<SavedTitleEntry>> ListSavedAsync(UserId userId, CancellationToken ct)
    {
        var rows = await db.SavedTitles
            .AsNoTracking()
            .Where(saved => saved.UserId == userId)
            .OrderByDescending(saved => saved.SavedAt)
            .Select(saved => new
            {
                saved.TitleId,
                saved.SavedAt,
            })
            .ToListAsync(ct);

        return rows.Select(row => new SavedTitleEntry(row.TitleId.Value, row.SavedAt)).ToList();
    }

    public async Task<IReadOnlyList<WatchProgressEntry>> ListHistoryAsync(UserId userId, CancellationToken ct)
    {
        var rows = await db.WatchHistory
            .AsNoTracking()
            .Where(progress => progress.UserId == userId)
            .OrderByDescending(progress => progress.UpdatedAt)
            .Select(progress => new
            {
                progress.TitleId,
                progress.PlayableId,
                progress.EpisodeNumber,
                progress.Position,
                progress.UpdatedAt,
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(row => row.TitleId)
            .Select(group =>
            {
                var row = group.First();
                return new WatchProgressEntry(
                    row.TitleId.Value,
                    row.PlayableId.Value,
                    row.EpisodeNumber,
                    row.Position.Seconds,
                    row.UpdatedAt);
            })
            .ToList();
    }
}
