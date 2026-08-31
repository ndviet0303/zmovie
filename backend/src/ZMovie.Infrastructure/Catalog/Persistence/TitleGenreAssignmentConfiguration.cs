using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Catalog;

namespace ZMovie.Infrastructure.Catalog.Persistence;

public sealed class TitleGenreAssignmentConfiguration : IEntityTypeConfiguration<TitleGenreAssignment>
{
    public void Configure(EntityTypeBuilder<TitleGenreAssignment> builder)
    {
        builder.ToTable("title_genres", "public");

        builder.HasKey(x => new { x.TitleId, x.GenreId }).HasName("pk_title_genres");

        builder.Property(x => x.TitleId)
            .HasConversion(id => id.Value, value => new TitleId(value))
            .HasColumnName("title_id")
            .IsRequired();

        builder.Property(x => x.GenreId)
            .HasConversion(id => id.Value, value => new GenreId(value))
            .HasColumnName("genre_id")
            .IsRequired();

        builder.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        builder.HasIndex(x => x.GenreId)
            .HasDatabaseName("ix_title_genres_genre_id");
    }
}
