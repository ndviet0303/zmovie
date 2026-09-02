namespace ZMovie.Domain.Identity;

public readonly record struct UserId(Guid Value) : IComparable<UserId>, IComparable
{
    public static UserId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(UserId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is UserId other ? CompareTo(other) : 1;
}
