using ZMovie.Domain.Common;

namespace ZMovie.Domain.Analytics;

public sealed record TitleViewRecordedDomainEvent(
    ViewEventId ViewEventId,
    TitleId TitleId,
    UserId? UserId,
    string SessionId,
    int? EpisodeNumber,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
