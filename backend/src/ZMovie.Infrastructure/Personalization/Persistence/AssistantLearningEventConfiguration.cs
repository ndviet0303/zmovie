using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZMovie.Domain.Personalization;

namespace ZMovie.Infrastructure.Personalization.Persistence;

public sealed class AssistantLearningEventConfiguration : IEntityTypeConfiguration<AssistantLearningEvent>
{
    public void Configure(EntityTypeBuilder<AssistantLearningEvent> builder)
    {
        builder.ToTable("assistant_learning_events", "public");

        builder.HasKey(x => x.Id).HasName("pk_assistant_learning_events");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new LearningEventId(value))
            .IsRequired();

        builder.Property(x => x.RecommendationId)
            .HasColumnName("recommendation_id")
            .HasConversion(id => id.Value, value => new RecommendationId(value))
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.TitleId)
            .HasColumnName("title_id")
            .HasConversion(id => id.Value, value => new TitleId(value))
            .IsRequired();

        builder.Property(x => x.Features)
            .HasColumnName("features")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Rank)
            .HasColumnName("rank")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Reward)
            .HasColumnName("reward")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.RecommendationId, x.TitleId })
            .HasDatabaseName("ix_assistant_learning_events_user_id_recommendation_id_title_id");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("ix_assistant_learning_events_user_id_created_at");
    }
}
