using ZMovie.Domain.Common;

namespace ZMovie.Domain.Personalization;

public sealed class AssistantLearningEvent : AggregateRoot, IEntity<LearningEventId>
{
    public const int MaxFeaturesLength = 2000;

    public LearningEventId Id { get; private set; }
    public RecommendationId RecommendationId { get; private set; }
    public UserId UserId { get; private set; }
    public TitleId TitleId { get; private set; }
    public string Features { get; private set; } = string.Empty;
    public int Rank { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public double Reward { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AssistantLearningEvent() { }

    public static AssistantLearningEvent RecordImpression(
        LearningEventId id,
        RecommendationId recommendationId,
        UserId userId,
        TitleId titleId,
        string features,
        int rank,
        DateTimeOffset createdAt)
    {
        ValidateCommon(id, recommendationId, userId, titleId, features, rank, createdAt);

        var learningEvent = new AssistantLearningEvent
        {
            Id = id,
            RecommendationId = recommendationId,
            UserId = userId,
            TitleId = titleId,
            Features = features,
            Rank = rank,
            EventType = FeedbackEventType.ImpressionName,
            Reward = 0.0,
            CreatedAt = createdAt,
        };

        learningEvent.RaiseDomainEvent(new AssistantImpressionRecordedDomainEvent(id, recommendationId, userId, titleId, features, rank, createdAt));
        return learningEvent;
    }

    public static AssistantLearningEvent RecordFeedback(
        LearningEventId id,
        RecommendationId recommendationId,
        UserId userId,
        TitleId titleId,
        string features,
        int rank,
        FeedbackEventType eventType,
        double reward,
        DateTimeOffset createdAt)
    {
        ValidateCommon(id, recommendationId, userId, titleId, features, rank, createdAt);

        if (eventType.IsImpression)
        {
            throw new ArgumentException("Feedback event cannot have 'impression' type.", nameof(eventType));
        }

        var learningEvent = new AssistantLearningEvent
        {
            Id = id,
            RecommendationId = recommendationId,
            UserId = userId,
            TitleId = titleId,
            Features = features,
            Rank = rank,
            EventType = eventType.Value,
            Reward = reward,
            CreatedAt = createdAt,
        };

        learningEvent.RaiseDomainEvent(new PersonalizationFeedbackRecordedDomainEvent(id, recommendationId, userId, titleId, features, rank, eventType, reward, createdAt));
        return learningEvent;
    }

    private static void ValidateCommon(
        LearningEventId id,
        RecommendationId recommendationId,
        UserId userId,
        TitleId titleId,
        string features,
        int rank,
        DateTimeOffset createdAt)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Learning event ID cannot be empty.", nameof(id));
        if (recommendationId.Value == Guid.Empty) throw new ArgumentException("Recommendation ID cannot be empty.", nameof(recommendationId));
        if (userId.Value == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (titleId.Value == Guid.Empty) throw new ArgumentException("Title ID cannot be empty.", nameof(titleId));
        if (rank < 1) throw new ArgumentException("Rank must be greater than or equal to 1.", nameof(rank));
        if (string.IsNullOrEmpty(features)) throw new ArgumentException("Features cannot be empty.", nameof(features));
        if (features.Length > MaxFeaturesLength) throw new ArgumentException($"Features length exceeds maximum of {MaxFeaturesLength}.", nameof(features));
        if (createdAt == default) throw new ArgumentException("Created timestamp must be a valid DateTimeOffset.", nameof(createdAt));
    }
}
