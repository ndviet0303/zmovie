using ZMovie.Domain.Common;

namespace ZMovie.Domain.Personalization;

public sealed record AssistantImpressionRecordedDomainEvent(
    LearningEventId LearningEventId,
    RecommendationId RecommendationId,
    UserId UserId,
    TitleId TitleId,
    string Features,
    int Rank,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record PersonalizationFeedbackRecordedDomainEvent(
    LearningEventId LearningEventId,
    RecommendationId RecommendationId,
    UserId UserId,
    TitleId TitleId,
    string Features,
    int Rank,
    FeedbackEventType EventType,
    double Reward,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
