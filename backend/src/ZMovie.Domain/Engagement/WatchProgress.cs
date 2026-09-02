using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public sealed class WatchProgress : AggregateRoot
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
        DateTimeOffset updatedAt)
    {
        var progress = new WatchProgress(userId, playableId, titleId, episodeNumber, position, updatedAt);
        progress.RaiseDomainEvent(new WatchProgressRecordedDomainEvent(userId, playableId, titleId, episodeNumber, position.Seconds, updatedAt));
        return progress;
    }

    public void UpdateProgress(int? episodeNumber, WatchPosition position, DateTimeOffset updatedAt)
    {
        EpisodeNumber = episodeNumber;
        Position = position;
        UpdatedAt = updatedAt;

        RaiseDomainEvent(new WatchProgressRecordedDomainEvent(UserId, PlayableId, TitleId, episodeNumber, position.Seconds, updatedAt));
    }
}
