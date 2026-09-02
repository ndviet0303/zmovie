using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Catalog;

namespace ZMovie.Infrastructure.Catalog.Persistence;

public sealed class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.ToTable("episodes", "public");

        builder.HasKey(x => x.Id).HasName("pk_episodes");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EpisodeId(value))
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.TitleId)
            .HasConversion(id => id.Value, value => new TitleId(value))
            .HasColumnName("title_id")
            .IsRequired();

        builder.Property(x => x.Number)
            .HasColumnName("number")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.HlsUrl)
            .HasColumnName("hls_url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.SubtitleUrl)
            .HasColumnName("subtitle_url")
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.HasIndex(x => new { x.TitleId, x.Number })
            .IsUnique()
            .HasDatabaseName("ix_episodes_title_id_number");
    }
}
