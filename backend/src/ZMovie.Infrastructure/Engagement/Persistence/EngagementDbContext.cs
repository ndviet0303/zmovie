using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Engagement.Persistence;

public sealed class EngagementDbContext(DbContextOptions<EngagementDbContext> options) : DbContext(options)
{
    public DbSet<SavedTitle> SavedTitles => Set<SavedTitle>();
    public DbSet<WatchProgress> WatchHistory => Set<WatchProgress>();
    public DbSet<Review> TitleReviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfiguration(new SavedTitleConfiguration());
        modelBuilder.ApplyConfiguration(new WatchProgressConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());
    }
}
