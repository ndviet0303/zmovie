using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Catalog;

namespace ZMovie.Infrastructure.Catalog.Persistence;

public sealed class TitleConfiguration : IEntityTypeConfiguration<Title>
{
    public void Configure(EntityTypeBuilder<Title> builder)
    {
        builder.ToTable("titles", "public");

        builder.HasKey(x => x.Id).HasName("pk_titles");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TitleId(value))
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasConversion(slug => slug.Value, value => TitleSlug.Parse(value))
            .HasColumnName("slug")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.VietnameseTitle)
            .HasColumnName("vietnamese_title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.EnglishTitle)
            .HasColumnName("english_title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.VietnameseSynopsis)
            .HasColumnName("vietnamese_synopsis")
            .IsRequired();

        builder.Property(x => x.EnglishSynopsis)
            .HasColumnName("english_synopsis")
            .IsRequired();

        builder.Ignore(x => x.TitleName);
        builder.Ignore(x => x.Synopsis);

        builder.Property(x => x.Genre)
            .HasColumnName("genre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Year)
            .HasConversion(year => year.Value, value => ReleaseYear.FromInt(value))
            .HasColumnName("year")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion(type => type.Value, value => TitleType.Normalize(value))
            .HasColumnName("type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PosterUrl)
            .HasColumnName("poster_url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Runtime)
            .HasConversion(runtime => runtime.Minutes, value => Runtime.FromMinutes(value))
            .HasColumnName("runtime_minutes")
            .IsRequired();

        builder.Property(x => x.Featured)
            .HasColumnName("featured")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ix_titles_slug");

        builder.HasIndex(x => new { x.Featured, x.CreatedAt })
            .HasDatabaseName("ix_titles_featured_created_at");

        builder.HasIndex(x => new { x.Genre, x.CreatedAt })
            .HasDatabaseName("ix_titles_genre_created_at");

        builder.HasIndex(x => new { x.Type, x.CreatedAt })
            .HasDatabaseName("ix_titles_type_created_at");

        builder.HasIndex(x => x.UpdatedAt)
            .HasDatabaseName("ix_titles_updated_at");
    }
}
