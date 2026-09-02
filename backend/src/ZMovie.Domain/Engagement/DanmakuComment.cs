using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public readonly record struct DanmakuCommentId(Guid Value) : IComparable<DanmakuCommentId>, IComparable
{
    public static DanmakuCommentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(DanmakuCommentId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is DanmakuCommentId other ? CompareTo(other) : 1;
}

public sealed class DanmakuComment : IEntity<DanmakuCommentId>
{
    private DanmakuComment() { }

    public DanmakuCommentId Id { get; private set; }
    public string TitleSlug { get; private set; } = string.Empty;
    public int EpisodeNumber { get; private set; }
    public int TimeSeconds { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#ffffff";
    public Guid? UserId { get; private set; }
    public string AuthorName { get; private set; } = "Anonymous";
    public DateTimeOffset CreatedAt { get; private set; }

    public static DanmakuComment Create(
        DanmakuCommentId id,
        string titleSlug,
        int episodeNumber,
        int timeSeconds,
        string content,
        string color,
        Guid? userId,
        string authorName,
        DateTimeOffset occurredAt)
    {
        return new DanmakuComment
        {
            Id = id,
            TitleSlug = titleSlug?.Trim() ?? string.Empty,
            EpisodeNumber = episodeNumber,
            TimeSeconds = Math.Max(0, timeSeconds),
            Content = content?.Trim() ?? string.Empty,
            Color = string.IsNullOrWhiteSpace(color) ? "#ffffff" : color.Trim(),
            UserId = userId,
            AuthorName = string.IsNullOrWhiteSpace(authorName) ? "Anonymous" : authorName.Trim(),
            CreatedAt = occurredAt
        };
    }
}
