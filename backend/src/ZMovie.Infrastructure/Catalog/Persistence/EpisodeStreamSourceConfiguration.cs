using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Catalog;

namespace ZMovie.Infrastructure.Catalog.Persistence;

public sealed class EpisodeStreamSourceConfiguration : IEntityTypeConfiguration<EpisodeStreamSource>
{
    public void Configure(EntityTypeBuilder<EpisodeStreamSource> builder)
    {
        builder.ToTable("episode_stream_sources", "public");

        builder.HasKey(x => x.Id).HasName("pk_episode_stream_sources");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EpisodeSourceId(value))
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.EpisodeId)
            .HasConversion(id => id.Value, value => new EpisodeId(value))
            .HasColumnName("episode_id")
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Format)
            .HasColumnName("format")
            .HasMaxLength(50)
            .HasDefaultValue("hls")
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.SubtitleUrl)
            .HasColumnName("subtitle_url")
            .HasMaxLength(2000);

        builder.Property(x => x.AudioTrack)
            .HasColumnName("audio_track")
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.EpisodeId, x.Priority })
            .HasDatabaseName("ix_episode_stream_sources_episode_priority");
    }
}
