using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Infrastructure.Persistence;

public sealed class LegacyCatalogDbContext(DbContextOptions<LegacyCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Title> Titles => Set<Title>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<TitleGenreAssignment> TitleGenres => Set<TitleGenreAssignment>();
    public DbSet<SavedTitle> SavedTitles => Set<SavedTitle>();
    public DbSet<WatchProgress> WatchHistory => Set<WatchProgress>();
    public DbSet<TitleViewEvent> TitleViewEvents => Set<TitleViewEvent>();
    public DbSet<Review> TitleReviews => Set<Review>();
    public DbSet<AssistantLearningEvent> AssistantLearningEvents => Set<AssistantLearningEvent>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfiguration(new TitleConfiguration());
        modelBuilder.ApplyConfiguration(new EpisodeConfiguration());
        modelBuilder.ApplyConfiguration(new GenreConfiguration());
        modelBuilder.ApplyConfiguration(new TitleGenreAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new SavedTitleConfiguration());
        modelBuilder.ApplyConfiguration(new WatchProgressConfiguration());
        modelBuilder.ApplyConfiguration(new TitleViewEventConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AssistantLearningEventConfiguration());

        // The legacy database schema up to 202608310001 does not include modern enriched columns
        modelBuilder.Entity<Title>().Ignore(x => x.Actors);
        modelBuilder.Entity<Title>().Ignore(x => x.Directors);
        modelBuilder.Entity<Title>().Ignore(x => x.Country);
        modelBuilder.Entity<Title>().Ignore(x => x.TrailerUrl);
        modelBuilder.Entity<Title>().Ignore(x => x.IsR2Hosted);
        modelBuilder.Entity<Episode>().Ignore(x => x.SubtitleUrl);
        modelBuilder.Ignore<EpisodeStreamSource>();
        modelBuilder.Entity<Episode>().Ignore(x => x.IntroStart);
        modelBuilder.Entity<Episode>().Ignore(x => x.IntroEnd);
        modelBuilder.Entity<Episode>().Ignore(x => x.OutroStart);
        modelBuilder.Entity<Episode>().Ignore(x => x.OutroEnd);
        modelBuilder.Entity<User>().Ignore(x => x.VipExpiresAt);
        modelBuilder.Entity<User>().Ignore(x => x.SubscriptionTier);
    }
}



