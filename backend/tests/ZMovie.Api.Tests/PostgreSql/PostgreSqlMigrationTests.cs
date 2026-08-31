using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;
using ZMovie.Infrastructure.Persistence;
using Xunit;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlMigrationTests(PostgreSqlFixture fixture)
{
    public const string LegacyHead = "202607270001_AddTitleUpdatedAtIndex";

    private static readonly string[] LegacyMigrationChain =
    [
        "202607210001_InitialCatalog",
        "202607210002_AddEpisodes",
        "202607220001_AddTitleViewEvents",
        "202607230001_AddGoogleUsers",
        "202607230002_AddUserLibrary",
        "202607230003_MoveUserLibraryToEngagement",
        "202607230004_AddPlayableProgress",
        "202607230005_MoveTitleViewEventsToEngagement",
        "202607230006_AddTitleReviews",
        "202607230007_AddCatalogGenres",
        "202607240001_MoveCatalogTablesToPublicSchema",
        "202607240002_MoveEngagementTablesToPublicSchema",
        "202607240003_DropLegacySchemas",
        "202607260001_AddAssistantLearningEvents",
        "202607260002_AddUserRoles",
        "202607260003_AddTitleIdIndexes",
        LegacyHead,
        "202608310001_AddTitleGenres",
    ];

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Fresh_database_migrates_the_complete_legacy_chain()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreateLegacyContext(database.ConnectionString);

        await context.Database.MigrateAsync();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(LegacyMigrationChain, applied.Take(LegacyMigrationChain.Length));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        await AssertCurrentLegacySchemaAsync(database.ConnectionString);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Fresh_database_migrates_all_module_contexts()
    {
        await using var database = await fixture.CreateDatabaseAsync();

        await using var catalog = CreateCatalogContext(database.ConnectionString);
        await using var identity = CreateIdentityContext(database.ConnectionString);
        await using var engagement = CreateEngagementContext(database.ConnectionString);
        await using var analytics = CreateAnalyticsContext(database.ConnectionString);
        await using var personalization = CreatePersonalizationContext(database.ConnectionString);

        await catalog.Database.MigrateAsync();
        await identity.Database.MigrateAsync();
        await engagement.Database.MigrateAsync();
        await analytics.Database.MigrateAsync();
        await personalization.Database.MigrateAsync();

        Assert.Empty(await catalog.Database.GetPendingMigrationsAsync());
        Assert.Empty(await identity.Database.GetPendingMigrationsAsync());
        Assert.Empty(await engagement.Database.GetPendingMigrationsAsync());
        Assert.Empty(await analytics.Database.GetPendingMigrationsAsync());
        Assert.Empty(await personalization.Database.GetPendingMigrationsAsync());

        await AssertAllModuleHistoryTablesAsync(database.ConnectionString);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Database_at_the_current_legacy_head_upgrades_without_losing_data()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var titleId = Guid.Parse("55555555-5555-4555-8555-555555555555");
        var genreId = Guid.Parse("66666666-6666-4666-8666-666666666666");
        var userId = Guid.Parse("77777777-7777-4777-8777-777777777777");

        await using (var legacy = CreateLegacyContext(database.ConnectionString))
        {
            var targetName = legacy.GetService<IMigrationsIdGenerator>().GetName(LegacyHead);
            Assert.Equal(LegacyHead, legacy.GetService<IMigrationsAssembly>().FindMigrationId(targetName));
            await legacy.Database.MigrateAsync(targetName);
            legacy.Titles.Add(Title.Create(
                new TitleId(titleId),
                TitleSlug.Parse("upgrade-baseline"),
                new LocalizedText("Dữ liệu nâng cấp", "Upgrade Baseline"),
                new LocalizedText("Dữ liệu cũ phải được giữ nguyên.", "Legacy data must survive."),
                "Drama",
                ReleaseYear.FromInt(2026),
                TitleType.Movie,
                "https://example.test/upgrade.jpg",
                Runtime.FromMinutes(90),
                false,
                DateTimeOffset.UtcNow));
            legacy.Genres.Add(Genre.Create(new GenreId(genreId), "drama", "Drama", DateTimeOffset.UtcNow));
            legacy.Users.Add(User.Create(
                new UserId(userId),
                new ExternalIdentity("upgrade-subject"),
                "upgrade@zmovie.test",
                "Upgrade User",
                null,
                Role.Member,
                DateTimeOffset.UtcNow));
            await legacy.SaveChangesAsync();
        }

        await using (var upgraded = CreateLegacyContext(database.ConnectionString))
        {
            await upgraded.Database.MigrateAsync();
            Assert.Empty(await upgraded.Database.GetPendingMigrationsAsync());
            Assert.Equal("Upgrade Baseline", (await upgraded.Titles.SingleAsync(x => x.Id == new TitleId(titleId))).TitleName.English);
            Assert.Equal("Drama", (await upgraded.Genres.SingleAsync(x => x.Id == new GenreId(genreId))).Name);
            Assert.Equal("upgrade@zmovie.test", (await upgraded.Users.SingleAsync(x => x.Id == new UserId(userId))).Email);

            var applied = (await upgraded.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.Equal(LegacyMigrationChain, applied.Take(LegacyMigrationChain.Length));
        }

        // Now also run module migrations on the upgraded database to verify seamless cutover
        await using var catalog = CreateCatalogContext(database.ConnectionString);
        await using var identity = CreateIdentityContext(database.ConnectionString);
        await using var engagement = CreateEngagementContext(database.ConnectionString);
        await using var analytics = CreateAnalyticsContext(database.ConnectionString);
        await using var personalization = CreatePersonalizationContext(database.ConnectionString);

        await catalog.Database.MigrateAsync();
        await identity.Database.MigrateAsync();
        await engagement.Database.MigrateAsync();
        await analytics.Database.MigrateAsync();
        await personalization.Database.MigrateAsync();

        Assert.Empty(await catalog.Database.GetPendingMigrationsAsync());
        Assert.Empty(await identity.Database.GetPendingMigrationsAsync());
        Assert.Empty(await engagement.Database.GetPendingMigrationsAsync());
        Assert.Empty(await analytics.Database.GetPendingMigrationsAsync());
        Assert.Empty(await personalization.Database.GetPendingMigrationsAsync());
    }

    private static LegacyCatalogDbContext CreateLegacyContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<LegacyCatalogDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new LegacyCatalogDbContext(options);
    }

    private static CatalogDbContext CreateCatalogContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__ef_migrations_history_catalog", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CatalogDbContext(options);
    }

    private static IdentityDbContext CreateIdentityContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__ef_migrations_history_identity", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new IdentityDbContext(options);
    }

    private static EngagementDbContext CreateEngagementContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EngagementDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__ef_migrations_history_engagement", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new EngagementDbContext(options);
    }

    private static AnalyticsDbContext CreateAnalyticsContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__ef_migrations_history_analytics", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AnalyticsDbContext(options);
    }

    private static PersonalizationDbContext CreatePersonalizationContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PersonalizationDbContext>()
            .UseNpgsql(connectionString, b => b.MigrationsHistoryTable("__ef_migrations_history_personalization", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PersonalizationDbContext(options);
    }

    private static async Task AssertAllModuleHistoryTablesAsync(string connectionString)
    {
        var expectedHistoryTables = new[]
        {
            "__ef_migrations_history_analytics",
            "__ef_migrations_history_catalog",
            "__ef_migrations_history_engagement",
            "__ef_migrations_history_identity",
            "__ef_migrations_history_personalization",
        };

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name LIKE '__ef_migrations_history_%' ORDER BY table_name",
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        Assert.Equal(expectedHistoryTables, tables);
    }

    private static async Task AssertCurrentLegacySchemaAsync(string connectionString)
    {
        var expectedTables = new[]
        {
            "__EFMigrationsHistory",
            "assistant_learning_events",
            "episodes",
            "genres",
            "saved_titles",
            "title_genres",
            "title_reviews",
            "title_view_events",
            "titles",
            "users",
            "watch_history",
        };

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name", connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            var tables = new List<string>();
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            Assert.Equal(expectedTables, tables);
        }

        await using var schemaCommand = new NpgsqlCommand("SELECT schema_name FROM information_schema.schemata WHERE schema_name IN ('catalog', 'engagement')", connection);
        await using var schemaReader = await schemaCommand.ExecuteReaderAsync();
        Assert.False(await schemaReader.ReadAsync());
    }
}
