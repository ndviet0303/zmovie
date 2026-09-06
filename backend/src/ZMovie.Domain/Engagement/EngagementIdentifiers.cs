namespace ZMovie.Domain.Engagement;

public readonly record struct ReviewId(Guid Value) : IComparable<ReviewId>, IComparable
{
    public static ReviewId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(ReviewId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is ReviewId other ? CompareTo(other) : 1;
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
