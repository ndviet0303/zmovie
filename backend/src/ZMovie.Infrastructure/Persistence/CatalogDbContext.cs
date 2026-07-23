using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<CatalogTitle> Titles => Set<CatalogTitle>();
    public DbSet<CatalogEpisode> Episodes => Set<CatalogEpisode>();
    public DbSet<CatalogGenre> Genres => Set<CatalogGenre>();
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
