namespace ZMovie.Domain.Engagement;

public sealed class SavedTitle
{
    public Guid UserId { get; init; }
    public Guid TitleId { get; init; }
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class WatchProgress
{
    public Guid UserId { get; init; }
    public Guid PlayableId { get; init; }
    public Guid TitleId { get; init; }
    public int? EpisodeNumber { get; set; }
    public double ProgressSeconds { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TitleViewEvent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TitleId { get; init; }
    public int? EpisodeNumber { get; init; }
    public Guid? UserId { get; init; }
    public required string SessionId { get; init; }
    public DateTimeOffset ViewedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class TitleReview
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TitleId { get; init; }
    public Guid UserId { get; init; }
    public required string AuthorName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
