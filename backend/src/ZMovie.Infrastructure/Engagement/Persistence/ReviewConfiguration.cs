using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Engagement.Persistence;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("title_reviews", "public");

        builder.HasKey(review => review.Id)
            .HasName("pk_title_reviews");

        builder.Property(review => review.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new ReviewId(value))
            .ValueGeneratedNever();

        builder.Property(review => review.TitleId)
            .HasColumnName("title_id")
            .HasConversion(id => id.Value, value => new TitleId(value));

        builder.Property(review => review.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(review => review.AuthorName)
            .HasColumnName("author_name")
            .HasMaxLength(Review.MaximumAuthorNameLength)
            .IsRequired();

        builder.Property(review => review.Rating)
            .HasColumnName("rating")
            .HasConversion(rating => rating.Value, value => MaterializeRating(value));

        builder.Property(review => review.Comment)
            .HasColumnName("comment")
            .HasMaxLength(Review.MaximumCommentLength);

        builder.Property(review => review.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(review => review.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(review => new { review.TitleId, review.UserId })
            .IsUnique()
            .HasDatabaseName("ix_title_reviews_title_id_user_id");

        builder.HasIndex(review => new { review.TitleId, review.UpdatedAt })
            .HasDatabaseName("ix_title_reviews_title_id_updated_at");
    }

    private static Rating MaterializeRating(int value) =>
        Rating.TryCreate(value, out var rating)
            ? rating
            : throw new InvalidOperationException($"Stored review rating '{value}' is outside the supported range.");
}
