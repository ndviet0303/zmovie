using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Engagement.Persistence;

public sealed class SavedTitleConfiguration : IEntityTypeConfiguration<SavedTitle>
{
    public void Configure(EntityTypeBuilder<SavedTitle> builder)
    {
        builder.ToTable("saved_titles", "public");

        builder.HasKey(saved => new { saved.UserId, saved.TitleId })
            .HasName("pk_saved_titles");

        builder.Property(saved => saved.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(saved => saved.TitleId)
            .HasColumnName("title_id")
            .HasConversion(id => id.Value, value => new TitleId(value));

        builder.Property(saved => saved.SavedAt)
            .HasColumnName("saved_at")
            .IsRequired();

        builder.HasIndex(saved => new { saved.UserId, saved.SavedAt })
            .HasDatabaseName("ix_saved_titles_user_id_saved_at");

        builder.HasIndex(saved => saved.TitleId)
            .HasDatabaseName("ix_saved_titles_title_id");
    }
}
