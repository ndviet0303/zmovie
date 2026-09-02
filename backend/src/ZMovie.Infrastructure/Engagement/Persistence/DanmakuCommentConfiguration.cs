using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Engagement.Persistence;

public sealed class DanmakuCommentConfiguration : IEntityTypeConfiguration<DanmakuComment>
{
    public void Configure(EntityTypeBuilder<DanmakuComment> builder)
    {
        builder.ToTable("danmaku_comments", "public");

        builder.HasKey(x => x.Id).HasName("pk_danmaku_comments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DanmakuCommentId(value))
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.TitleSlug)
            .HasColumnName("title_slug")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.EpisodeNumber)
            .HasColumnName("episode_number")
            .IsRequired();

        builder.Property(x => x.TimeSeconds)
            .HasColumnName("time_seconds")
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Color)
            .HasColumnName("color")
            .HasMaxLength(20)
            .HasDefaultValue("#ffffff")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.AuthorName)
            .HasColumnName("author_name")
            .HasMaxLength(100)
            .HasDefaultValue("Anonymous")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new { x.TitleSlug, x.EpisodeNumber, x.TimeSeconds })
            .HasDatabaseName("ix_danmaku_slug_episode_time");
    }
}
