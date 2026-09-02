using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Analytics;

namespace ZMovie.Infrastructure.Analytics.Persistence;

public sealed class TitleViewEventConfiguration : IEntityTypeConfiguration<TitleViewEvent>
{
    public void Configure(EntityTypeBuilder<TitleViewEvent> builder)
    {
        builder.ToTable("title_view_events", "public");

        builder.HasKey(x => x.Id).HasName("pk_title_view_events");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ViewEventId(value))
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.TitleId)
            .HasConversion(id => id.Value, value => new TitleId(value))
            .HasColumnName("title_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new UserId(value.Value) : null)
            .HasColumnName("user_id");

        builder.Property(x => x.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EpisodeNumber)
            .HasColumnName("episode_number");

        builder.Property(x => x.ViewedAt)
            .HasColumnName("viewed_at")
            .IsRequired();

        builder.HasIndex(x => x.TitleId)
            .HasDatabaseName("ix_title_view_events_title_id");

        builder.HasIndex(x => x.ViewedAt)
            .HasDatabaseName("ix_title_view_events_viewed_at");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_title_view_events_user_id");

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("ix_title_view_events_session_id");
    }
}
