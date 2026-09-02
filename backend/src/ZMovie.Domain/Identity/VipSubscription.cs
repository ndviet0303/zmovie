using ZMovie.Domain.Common;

namespace ZMovie.Domain.Identity;

public readonly record struct VipSubscriptionId(Guid Value) : IComparable<VipSubscriptionId>, IComparable
{
    public static VipSubscriptionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(VipSubscriptionId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is VipSubscriptionId other ? CompareTo(other) : 1;
}

public sealed class VipSubscription : IEntity<VipSubscriptionId>
{
    private VipSubscription() { }

    public VipSubscriptionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public decimal AmountVnd { get; private set; }
    public string PaymentGateway { get; private set; } = string.Empty;
    public string TransactionReference { get; private set; } = string.Empty;
    public DateTimeOffset PaidAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsFulfilled { get; private set; }

    public static VipSubscription Create(
        VipSubscriptionId id,
        UserId userId,
        string planCode,
        decimal amountVnd,
        string paymentGateway,
        string transactionReference,
        DateTimeOffset paidAt,
        DateTimeOffset expiresAt)
    {
        return new VipSubscription
        {
            Id = id,
            UserId = userId,
            PlanCode = planCode?.Trim() ?? "VIP1",
            AmountVnd = amountVnd,
            PaymentGateway = paymentGateway?.Trim() ?? "VietQR",
            TransactionReference = transactionReference?.Trim() ?? string.Empty,
            PaidAt = paidAt,
            ExpiresAt = expiresAt,
            IsFulfilled = true
        };
    }
}
