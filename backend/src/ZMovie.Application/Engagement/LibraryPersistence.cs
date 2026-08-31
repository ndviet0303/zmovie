using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public interface ISavedTitleRepository
{
    Task<SavedTitle?> FindAsync(UserId userId, TitleId titleId, CancellationToken ct);
    void Add(SavedTitle savedTitle);
    void Remove(SavedTitle savedTitle);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IWatchProgressRepository
{
    Task<WatchProgress?> FindAsync(UserId userId, PlayableId playableId, CancellationToken ct);
    void Add(WatchProgress progress);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IUserLibraryQueries
{
    Task<IReadOnlyList<SavedTitleEntry>> ListSavedAsync(UserId userId, CancellationToken ct);
    Task<IReadOnlyList<WatchProgressEntry>> ListHistoryAsync(UserId userId, CancellationToken ct);
}
