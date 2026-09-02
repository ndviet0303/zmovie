using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Analytics;

namespace ZMovie.Infrastructure.Analytics.Persistence;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<TitleViewEvent> TitleViewEvents => Set<TitleViewEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfiguration(new TitleViewEventConfiguration());
    }
}
