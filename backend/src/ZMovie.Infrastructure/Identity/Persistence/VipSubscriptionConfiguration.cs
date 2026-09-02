using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Identity.Persistence;

public sealed class VipSubscriptionConfiguration : IEntityTypeConfiguration<VipSubscription>
{
    public void Configure(EntityTypeBuilder<VipSubscription> builder)
    {
        builder.ToTable("vip_subscriptions", "public");

        builder.HasKey(x => x.Id).HasName("pk_vip_subscriptions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new VipSubscriptionId(value))
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.PlanCode)
            .HasColumnName("plan_code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.AmountVnd)
            .HasColumnName("amount_vnd")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaymentGateway)
            .HasColumnName("payment_gateway")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TransactionReference)
            .HasColumnName("transaction_reference")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnName("paid_at")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.IsFulfilled)
            .HasColumnName("is_fulfilled")
            .IsRequired();

        builder.HasIndex(x => new { x.PaymentGateway, x.TransactionReference })
            .IsUnique()
            .HasDatabaseName("ix_vip_subscriptions_gateway_reference");
    }
}
