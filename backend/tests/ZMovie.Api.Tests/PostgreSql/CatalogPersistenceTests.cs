using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;
using Xunit;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class CatalogPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Ef_mapping_matches_the_catalog_tables_schema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.MigrateAsync();

        var titleEntity = context.Model.FindEntityType(typeof(Title));
        titleEntity.Should().NotBeNull();
        titleEntity!.GetTableName().Should().Be("titles");

        var episodeEntity = context.Model.FindEntityType(typeof(Episode));
        episodeEntity.Should().NotBeNull();
        episodeEntity!.GetTableName().Should().Be("episodes");

        var genreEntity = context.Model.FindEntityType(typeof(Genre));
        genreEntity.Should().NotBeNull();
        genreEntity!.GetTableName().Should().Be("genres");

        var assignmentEntity = context.Model.FindEntityType(typeof(TitleGenreAssignment));
        assignmentEntity.Should().NotBeNull();
        assignmentEntity!.GetTableName().Should().Be("title_genres");

        var tables = await ReadTablesAsync(database.ConnectionString);
        tables.Should().Contain(["titles", "episodes", "genres", "title_genres"]);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Title_episode_and_genre_roundtrip_materializes_private_state_and_typed_ids()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var titleId = TitleId.New();
        var episodeId = EpisodeId.New();
        var genreId = GenreId.New();
        var titleSlug = TitleSlug.Parse("roundtrip-movie");
        var titleName = new LocalizedText("Phim Roundtrip", "Roundtrip Movie");
        var synopsis = new LocalizedText("Mô tả phim", "Movie synopsis");

        var title = Title.Create(
            titleId,
            titleSlug,
            titleName,
            synopsis,
            "Action, Sci-Fi",
            ReleaseYear.FromInt(2026),
            TitleType.Movie,
            "https://cdn.example/poster.jpg",
            Runtime.FromMinutes(125),
            true,
            Timestamp);

        var episode = Episode.Create(
            episodeId,
            titleId,
            1,
            "Tập 1",
            "https://video.example/1.m3u8");

        var genre = Genre.Create(
            genreId,
            "action",
            "Action",
            Timestamp);

        var assignment = TitleGenreAssignment.Create(titleId, genreId, Timestamp);

        await using (var writeContext = CreateContext(database.ConnectionString))
        {
            await writeContext.Database.MigrateAsync();
            var titleRepo = new EfTitleRepository(writeContext);
            var episodeRepo = new EfEpisodeRepository(writeContext);
            var genreRepo = new EfGenreRepository(writeContext);

            titleRepo.Add(title);
            episodeRepo.Add(episode);
            genreRepo.Add(genre);
            writeContext.TitleGenres.Add(assignment);

            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext(database.ConnectionString);
        var readTitleRepo = new EfTitleRepository(readContext);
        var readEpisodeRepo = new EfEpisodeRepository(readContext);
        var readGenreRepo = new EfGenreRepository(readContext);

        var loadedTitle = await readTitleRepo.FindByIdAsync(titleId, default);
        loadedTitle.Should().NotBeNull();
        loadedTitle!.Id.Should().Be(titleId);
        loadedTitle.Slug.Should().Be(titleSlug);
        loadedTitle.TitleName.Should().Be(titleName);
        loadedTitle.Synopsis.Should().Be(synopsis);
        loadedTitle.Genre.Should().Be("Action, Sci-Fi");
        loadedTitle.Year.Value.Should().Be(2026);
        loadedTitle.Type.Should().Be(TitleType.Movie);
        loadedTitle.PosterUrl.Should().Be("https://cdn.example/poster.jpg");
        loadedTitle.Runtime.Minutes.Should().Be(125);
        loadedTitle.Featured.Should().BeTrue();
        loadedTitle.CreatedAt.Should().Be(Timestamp);
        loadedTitle.UpdatedAt.Should().Be(Timestamp);

        var loadedEpisode = await readEpisodeRepo.FindByNumberAsync(titleId, 1, default);
        loadedEpisode.Should().NotBeNull();
        loadedEpisode!.Id.Should().Be(episodeId);
        loadedEpisode.TitleId.Should().Be(titleId);
        loadedEpisode.Number.Should().Be(1);
        loadedEpisode.Name.Should().Be("Tập 1");
        loadedEpisode.HlsUrl.Should().Be("https://video.example/1.m3u8");

        var loadedGenre = await readGenreRepo.FindByIdAsync(genreId, default);
        loadedGenre.Should().NotBeNull();
        loadedGenre!.Id.Should().Be(genreId);
        loadedGenre.Slug.Should().Be("action");
        loadedGenre.Name.Should().Be("Action");

        var assignedGenreIds = await readTitleRepo.GetAssignedGenreIdsAsync(titleId, default);
        assignedGenreIds.Should().Equal([genreId]);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Title_repository_syncs_genre_assignments_additively_and_destructively()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var titleId = TitleId.New();
        var genre1 = GenreId.New();
        var genre2 = GenreId.New();
        var genre3 = GenreId.New();

        await using (var setupContext = CreateContext(database.ConnectionString))
        {
            await setupContext.Database.MigrateAsync();
            var title = Title.Create(
                titleId,
                TitleSlug.Parse("sync-genres-movie"),
                new LocalizedText("Phim Thử", "Sync Movie"),
                new LocalizedText("Mô tả", "Synopsis"),
                "Drama",
                ReleaseYear.FromInt(2026),
                TitleType.Movie,
                "https://poster.jpg",
                Runtime.FromMinutes(90),
                false,
                Timestamp);
            setupContext.Titles.Add(title);
            setupContext.Genres.AddRange(
                Genre.Create(genre1, "g1", "G1", Timestamp),
                Genre.Create(genre2, "g2", "G2", Timestamp),
                Genre.Create(genre3, "g3", "G3", Timestamp));
            await setupContext.SaveChangesAsync();
        }

        await using (var syncContext = CreateContext(database.ConnectionString))
        {
            var repo = new EfTitleRepository(syncContext);
            await repo.SyncGenreAssignmentsAsync(titleId, [genre1, genre2], Timestamp, default);
        }

        await using (var verifyContext = CreateContext(database.ConnectionString))
        {
            var repo = new EfTitleRepository(verifyContext);
            var assigned = await repo.GetAssignedGenreIdsAsync(titleId, default);
            assigned.Should().BeEquivalentTo([genre1, genre2]);

            // Sync to genre2 and genre3 (removes genre1, keeps genre2, adds genre3)
            await repo.SyncGenreAssignmentsAsync(titleId, [genre2, genre3], Timestamp.AddMinutes(5), default);
        }

        await using (var finalContext = CreateContext(database.ConnectionString))
        {
            var repo = new EfTitleRepository(finalContext);
            var assigned = await repo.GetAssignedGenreIdsAsync(titleId, default);
            assigned.Should().BeEquivalentTo([genre2, genre3]);
        }
    }

    private static CatalogDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CatalogDbContext(options);
    }

    private static async Task<List<string>> ReadTablesAsync(string connectionString)
    {
        var tables = new List<string>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        return tables;
    }
}
