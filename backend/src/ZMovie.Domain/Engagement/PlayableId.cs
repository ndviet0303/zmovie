namespace ZMovie.Domain.Engagement;

public readonly record struct PlayableId(Guid Value) : IComparable<PlayableId>, IComparable
{
    public static PlayableId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(PlayableId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is PlayableId other ? CompareTo(other) : 1;
}
