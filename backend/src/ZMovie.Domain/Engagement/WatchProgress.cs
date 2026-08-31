namespace ZMovie.Domain.Engagement;

public sealed class WatchProgress
{
    private WatchProgress() { }

    private WatchProgress(
        UserId userId,
        PlayableId playableId,
        TitleId titleId,
        int? episodeNumber,
        WatchPosition position,
        DateTimeOffset updatedAt)
    {
        UserId = userId;
        PlayableId = playableId;
        TitleId = titleId;
        EpisodeNumber = episodeNumber;
        Position = position;
        UpdatedAt = updatedAt;
    }

    public UserId UserId { get; private set; }
    public PlayableId PlayableId { get; private set; }
    public TitleId TitleId { get; private set; }
    public int? EpisodeNumber { get; private set; }
    public WatchPosition Position { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static WatchProgress Record(
        UserId userId,
        PlayableId playableId,
        TitleId titleId,
        int? episodeNumber,
        WatchPosition position,
        DateTimeOffset updatedAt) =>
        new(userId, playableId, titleId, episodeNumber, position, updatedAt);

    public void UpdateProgress(int? episodeNumber, WatchPosition position, DateTimeOffset updatedAt)
    {
        EpisodeNumber = episodeNumber;
        Position = position;
        UpdatedAt = updatedAt;
    }
}
