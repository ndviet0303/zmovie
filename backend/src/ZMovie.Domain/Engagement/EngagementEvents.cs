using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public sealed record ReviewSubmittedDomainEvent(
    ReviewId ReviewId,
    TitleId TitleId,
    UserId UserId,
    int Rating,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record ReviewEditedDomainEvent(
    ReviewId ReviewId,
    TitleId TitleId,
    UserId UserId,
    int OldRating,
    int NewRating,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record TitleSavedDomainEvent(
    UserId UserId,
    TitleId TitleId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record WatchProgressRecordedDomainEvent(
    UserId UserId,
    PlayableId PlayableId,
    TitleId TitleId,
    int? EpisodeNumber,
    double ProgressSeconds,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
