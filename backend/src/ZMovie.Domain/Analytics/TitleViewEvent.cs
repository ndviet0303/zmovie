using ZMovie.Domain.Common;

namespace ZMovie.Domain.Analytics;

public sealed class TitleViewEvent : AggregateRoot, IEntity<ViewEventId>
{
    private TitleViewEvent() { }

    public ViewEventId Id { get; private set; }
    public TitleId TitleId { get; private set; }
    public int? EpisodeNumber { get; private set; }
    public UserId? UserId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public DateTimeOffset ViewedAt { get; private set; }

    public static TitleViewEvent Record(
        ViewEventId id,
        TitleId titleId,
        int? episodeNumber,
        UserId? userId,
        string sessionId,
        DateTimeOffset viewedAt)
    {
        if (episodeNumber.HasValue && episodeNumber.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(episodeNumber), "Episode number must be positive.");
        }

        var normalizedSessionId = sessionId?.Trim() ?? string.Empty;
        if (userId is null && string.IsNullOrEmpty(normalizedSessionId))
        {
            throw new ArgumentException("Session ID is required for anonymous view events.", nameof(sessionId));
        }

        if (viewedAt == default)
        {
            throw new ArgumentException("Viewed timestamp must be specified.", nameof(viewedAt));
        }

        var viewEvent = new TitleViewEvent
        {
            Id = id,
            TitleId = titleId,
            EpisodeNumber = episodeNumber,
            UserId = userId,
            SessionId = normalizedSessionId,
            ViewedAt = viewedAt,
        };

        viewEvent.RaiseDomainEvent(new TitleViewRecordedDomainEvent(id, titleId, userId, normalizedSessionId, episodeNumber, viewedAt));
        return viewEvent;
    }
}
