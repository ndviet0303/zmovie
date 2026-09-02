using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Engagement.Persistence;

public sealed class WatchProgressConfiguration : IEntityTypeConfiguration<WatchProgress>
{
    public void Configure(EntityTypeBuilder<WatchProgress> builder)
    {
        builder.ToTable("watch_history", "public");

        builder.HasKey(progress => new { progress.UserId, progress.PlayableId })
            .HasName("pk_watch_history");

        builder.Property(progress => progress.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(progress => progress.PlayableId)
            .HasColumnName("playable_id")
            .HasConversion(id => id.Value, value => new PlayableId(value));

        builder.Property(progress => progress.TitleId)
            .HasColumnName("title_id")
            .HasConversion(id => id.Value, value => new TitleId(value));

        builder.Property(progress => progress.EpisodeNumber)
            .HasColumnName("episode_number");

        builder.Property(progress => progress.Position)
            .HasColumnName("progress_seconds")
            .HasConversion(position => position.Seconds, value => WatchPosition.FromSeconds(value));

        builder.Property(progress => progress.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(progress => new { progress.UserId, progress.UpdatedAt })
            .HasDatabaseName("ix_watch_history_user_id_updated_at");

        builder.HasIndex(progress => new { progress.UserId, progress.TitleId, progress.UpdatedAt })
            .HasDatabaseName("ix_watch_history_user_id_title_id_updated_at");

        builder.HasIndex(progress => progress.TitleId)
            .HasDatabaseName("ix_watch_history_title_id");
    }
}
