namespace ZMovie.Domain.Analytics;

public readonly record struct ViewEventId(Guid Value) : IComparable<ViewEventId>, IComparable
{
    public static ViewEventId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(ViewEventId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is ViewEventId other ? CompareTo(other) : 1;
}

public readonly record struct TitleId(Guid Value) : IComparable<TitleId>, IComparable
{
    public static TitleId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(TitleId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is TitleId other ? CompareTo(other) : 1;
}

public readonly record struct UserId(Guid Value) : IComparable<UserId>, IComparable
{
    public static UserId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(UserId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is UserId other ? CompareTo(other) : 1;
}

public readonly record struct PlayableId(Guid Value) : IComparable<PlayableId>, IComparable
{
    public static PlayableId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(PlayableId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is PlayableId other ? CompareTo(other) : 1;
}
