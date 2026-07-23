using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Persistence;

public sealed class EngagementDbContext(DbContextOptions<EngagementDbContext> options) : DbContext(options)
{
    public DbSet<SavedTitle> SavedTitles => Set<SavedTitle>();
    public DbSet<WatchProgress> WatchHistory => Set<WatchProgress>();
    public DbSet<TitleViewEvent> TitleViewEvents => Set<TitleViewEvent>();
    public DbSet<TitleReview> TitleReviews => Set<TitleReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        var saved = modelBuilder.Entity<SavedTitle>();
        saved.ToTable("saved_titles");
        saved.HasKey(x => new { x.UserId, x.TitleId });
        saved.HasIndex(x => new { x.UserId, x.SavedAt });

        var history = modelBuilder.Entity<WatchProgress>();
        history.ToTable("watch_history");
        history.HasKey(x => new { x.UserId, x.PlayableId });
        history.Property(x => x.PlayableId).IsRequired();
        history.HasIndex(x => new { x.UserId, x.UpdatedAt });
        history.HasIndex(x => new { x.UserId, x.TitleId, x.UpdatedAt });

        var view = modelBuilder.Entity<TitleViewEvent>();
        view.ToTable("title_view_events");
        view.HasKey(x => x.Id);
        view.Property(x => x.SessionId).HasMaxLength(128).IsRequired();
        view.HasIndex(x => new { x.ViewedAt, x.TitleId });
        view.HasIndex(x => new { x.TitleId, x.UserId, x.SessionId, x.EpisodeNumber, x.ViewedAt });

        var review = modelBuilder.Entity<TitleReview>();
        review.ToTable("title_reviews");
        review.HasKey(x => x.Id);
        review.Property(x => x.AuthorName).HasMaxLength(300).IsRequired();
        review.Property(x => x.Comment).HasMaxLength(2_000);
        review.HasIndex(x => new { x.TitleId, x.UserId }).IsUnique();
        review.HasIndex(x => new { x.TitleId, x.UpdatedAt });
    }
}
