using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Identity.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "public");

        builder.HasKey(user => user.Id)
            .HasName("pk_users");

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(user => user.ExternalIdentity)
            .HasColumnName("google_subject")
            .HasMaxLength(128)
            .IsRequired()
            .HasConversion(identity => identity.Subject, value => new ExternalIdentity(value));

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(user => user.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(Role.Member)
            .HasConversion(role => role.Value, value => Role.Normalize(value));

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(user => user.LastSignedInAt)
            .HasColumnName("last_signed_in_at")
            .IsRequired();

        builder.HasIndex(user => user.ExternalIdentity)
            .IsUnique()
            .HasDatabaseName("ix_users_google_subject");

        builder.HasIndex(user => user.Email)
            .HasDatabaseName("ix_users_email");

        builder.HasIndex(user => new { user.Role, user.CreatedAt })
            .HasDatabaseName("ix_users_role_created_at");

        builder.Property(user => user.VipExpiresAt)
            .HasColumnName("vip_expires_at");

        builder.Property(user => user.SubscriptionTier)
            .HasColumnName("subscription_tier")
            .HasMaxLength(50)
            .HasDefaultValue("Free")
            .IsRequired();
    }
}

