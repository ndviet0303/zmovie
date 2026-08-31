namespace ZMovie.Domain.Personalization;

public readonly record struct LearningEventId(Guid Value) : IComparable<LearningEventId>, IComparable
{
    public static LearningEventId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(LearningEventId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is LearningEventId other ? CompareTo(other) : 1;
}

public readonly record struct RecommendationId(Guid Value) : IComparable<RecommendationId>, IComparable
{
    public static RecommendationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(RecommendationId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is RecommendationId other ? CompareTo(other) : 1;
}

public readonly record struct UserId(Guid Value) : IComparable<UserId>, IComparable
{
    public static UserId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(UserId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is UserId other ? CompareTo(other) : 1;
}

public readonly record struct TitleId(Guid Value) : IComparable<TitleId>, IComparable
{
    public static TitleId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(TitleId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is TitleId other ? CompareTo(other) : 1;
}
