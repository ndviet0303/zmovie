using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public readonly record struct ShortClipId(Guid Value) : IComparable<ShortClipId>, IComparable
{
    public static ShortClipId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(ShortClipId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is ShortClipId other ? CompareTo(other) : 1;
}

public sealed class ShortClip : IEntity<ShortClipId>
{
    private ShortClip() { }

    public ShortClipId Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string VideoUrl { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public string TargetMovieSlug { get; private set; } = string.Empty;
    public int LikesCount { get; private set; }
    public int SharesCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ShortClip Create(
        ShortClipId id,
        string title,
        string videoUrl,
        string thumbnailUrl,
        string targetMovieSlug,
        DateTimeOffset occurredAt)
    {
        return new ShortClip
        {
            Id = id,
            Title = title?.Trim() ?? string.Empty,
            VideoUrl = videoUrl?.Trim() ?? string.Empty,
            ThumbnailUrl = thumbnailUrl?.Trim() ?? string.Empty,
            TargetMovieSlug = targetMovieSlug?.Trim() ?? string.Empty,
            LikesCount = 0,
            SharesCount = 0,
            CreatedAt = occurredAt
        };
    }

    public void IncrementLikes() => LikesCount++;
    public void IncrementShares() => SharesCount++;
}

public static class BilibiliLevelCalculator
{
    public static (int Level, string Title, int CurrentExp, int NextLevelExp) Calculate(int totalExp)
    {
        var exp = Math.Max(0, totalExp);
        return exp switch
        {
            < 100 => (1, "Tân thủ", exp, 100),
            < 500 => (2, "Mọt phim", exp, 500),
            < 1500 => (3, "Ghiền phim", exp, 1500),
            < 4000 => (4, "Đại sư điện ảnh", exp, 4000),
            < 10000 => (5, "Huyền thoại ZMovie", exp, 10000),
            _ => (6, "Chúa tể Rạp chiếu", exp, 10000)
        };
    }
}
