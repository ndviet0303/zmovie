using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public readonly record struct UserExpLedgerId(Guid Value) : IComparable<UserExpLedgerId>, IComparable
{
    public static UserExpLedgerId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(UserExpLedgerId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is UserExpLedgerId other ? CompareTo(other) : 1;
}

public sealed class UserExpLedger : IEntity<UserExpLedgerId>
{
    private UserExpLedger() { }

    public UserExpLedgerId Id { get; private set; }
    public Guid UserId { get; private set; }
    public int ExpGained { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static UserExpLedger Create(
        UserExpLedgerId id,
        Guid userId,
        int expGained,
        string reason,
        DateTimeOffset occurredAt)
    {
        return new UserExpLedger
        {
            Id = id,
            UserId = userId,
            ExpGained = Math.Max(1, expGained),
            Reason = reason?.Trim() ?? "General",
            CreatedAt = occurredAt
        };
    }
}
