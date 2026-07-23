using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<CatalogTitle> Titles => Set<CatalogTitle>();
    public DbSet<CatalogEpisode> Episodes => Set<CatalogEpisode>();
    public DbSet<CatalogGenre> Genres => Set<CatalogGenre>();
    public DbSet<SavedTitle> SavedTitles => Set<SavedTitle>();
    public DbSet<WatchProgress> WatchHistory => Set<WatchProgress>();
    public DbSet<TitleViewEvent> TitleViewEvents => Set<TitleViewEvent>();
    public DbSet<TitleReview> TitleReviews => Set<TitleReview>();
    public DbSet<ZMovieUser> Users => Set<ZMovieUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        var title = modelBuilder.Entity<CatalogTitle>();
        title.ToTable("titles");
        title.HasKey(x => x.Id);
        title.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        title.HasIndex(x => x.Slug).IsUnique();
        title.Property(x => x.EnglishTitle).HasMaxLength(300).IsRequired();
        title.Property(x => x.VietnameseTitle).HasMaxLength(300).IsRequired();
        title.Property(x => x.EnglishSynopsis).HasMaxLength(4000).IsRequired();
        title.Property(x => x.VietnameseSynopsis).HasMaxLength(4000).IsRequired();
        title.Property(x => x.Genre).HasMaxLength(100).IsRequired();
        title.Property(x => x.Type).HasMaxLength(32).IsRequired();
        title.Property(x => x.PosterUrl).HasMaxLength(2000).IsRequired();
        title.Property(x => x.UpdatedAt).IsConcurrencyToken();
        var episode = modelBuilder.Entity<CatalogEpisode>();
        episode.ToTable("episodes");
        episode.HasKey(x => x.Id);
        episode.Property(x => x.Name).HasMaxLength(300).IsRequired();
        episode.Property(x => x.HlsUrl).HasMaxLength(2000).IsRequired();
        episode.HasIndex(x => new { x.TitleId, x.Number }).IsUnique();

        var genre = modelBuilder.Entity<CatalogGenre>();
        genre.ToTable("genres");
        genre.HasKey(x => x.Id);
        genre.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        genre.Property(x => x.Name).HasMaxLength(100).IsRequired();
        genre.HasIndex(x => x.Slug).IsUnique();

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

        var user = modelBuilder.Entity<ZMovieUser>();
        user.ToTable("users");
        user.HasKey(x => x.Id);
        user.Property(x => x.GoogleSubject).HasMaxLength(128).IsRequired();
        user.HasIndex(x => x.GoogleSubject).IsUnique();
        user.Property(x => x.Email).HasMaxLength(320).IsRequired();
        user.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        user.Property(x => x.AvatarUrl).HasMaxLength(2000);

    }
}
