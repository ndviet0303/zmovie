namespace ZMovie.Domain.Engagement;

public sealed class AssistantLearningEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid RecommendationId { get; init; }
    public Guid UserId { get; init; }
    public Guid TitleId { get; init; }
    public required string Features { get; init; }
    public int Rank { get; init; }
    public required string EventType { get; init; }
    public double Reward { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
