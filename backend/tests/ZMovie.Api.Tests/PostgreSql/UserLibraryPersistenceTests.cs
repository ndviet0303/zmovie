using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;
using Xunit;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class UserLibraryPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 31, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Ef_mapping_matches_the_existing_saved_titles_and_watch_history_schema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.MigrateAsync();

        // Check SavedTitle entity
        var savedEntity = context.Model.FindEntityType(typeof(SavedTitle));
        savedEntity.Should().NotBeNull();
        savedEntity!.GetTableName().Should().Be("saved_titles");
        savedEntity.GetSchema().Should().Be("public");

        var savedTable = StoreObjectIdentifier.Table("saved_titles", "public");
        savedEntity.FindPrimaryKey()!.Properties.Select(p => p.Name)
            .Should().Equal(nameof(SavedTitle.UserId), nameof(SavedTitle.TitleId));
        savedEntity.FindPrimaryKey()!.GetName(savedTable).Should().Be("pk_saved_titles");

        var savedIndexes = savedEntity.GetIndexes().ToDictionary(i => i.GetDatabaseName()!);
        savedIndexes["ix_saved_titles_user_id_saved_at"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(SavedTitle.UserId), nameof(SavedTitle.SavedAt));
        savedIndexes["ix_saved_titles_title_id"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(SavedTitle.TitleId));

        AssertProperty(savedEntity, savedTable, nameof(SavedTitle.UserId), "user_id", typeof(UserId), typeof(Guid), false, null);
        AssertProperty(savedEntity, savedTable, nameof(SavedTitle.TitleId), "title_id", typeof(TitleId), typeof(Guid), false, null);
        AssertProperty(savedEntity, savedTable, nameof(SavedTitle.SavedAt), "saved_at", typeof(DateTimeOffset), typeof(DateTimeOffset), false, null);

        // Check WatchProgress entity
        var progressEntity = context.Model.FindEntityType(typeof(WatchProgress));
        progressEntity.Should().NotBeNull();
        progressEntity!.GetTableName().Should().Be("watch_history");
        progressEntity.GetSchema().Should().Be("public");

        var progressTable = StoreObjectIdentifier.Table("watch_history", "public");
        progressEntity.FindPrimaryKey()!.Properties.Select(p => p.Name)
            .Should().Equal(nameof(WatchProgress.UserId), nameof(WatchProgress.PlayableId));
        progressEntity.FindPrimaryKey()!.GetName(progressTable).Should().Be("pk_watch_history");

        var progressIndexes = progressEntity.GetIndexes().ToDictionary(i => i.GetDatabaseName()!);
        progressIndexes["ix_watch_history_user_id_updated_at"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(WatchProgress.UserId), nameof(WatchProgress.UpdatedAt));
        progressIndexes["ix_watch_history_user_id_title_id_updated_at"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(WatchProgress.UserId), nameof(WatchProgress.TitleId), nameof(WatchProgress.UpdatedAt));
        progressIndexes["ix_watch_history_title_id"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(WatchProgress.TitleId));

        AssertProperty(progressEntity, progressTable, nameof(WatchProgress.UserId), "user_id", typeof(UserId), typeof(Guid), false, null);
        AssertProperty(progressEntity, progressTable, nameof(WatchProgress.PlayableId), "playable_id", typeof(PlayableId), typeof(Guid), false, null);
        AssertProperty(progressEntity, progressTable, nameof(WatchProgress.TitleId), "title_id", typeof(TitleId), typeof(Guid), false, null);
        AssertProperty(progressEntity, progressTable, nameof(WatchProgress.EpisodeNumber), "episode_number", typeof(int?), typeof(int?), true, null);
        AssertProperty(progressEntity, progressTable, nameof(WatchProgress.Position), "progress_seconds", typeof(WatchPosition), typeof(double), false, null);
        AssertProperty(progressEntity, progressTable, nameof(WatchProgress.UpdatedAt), "updated_at", typeof(DateTimeOffset), typeof(DateTimeOffset), false, null);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task SavedTitle_repository_and_queries_roundtrip_on_postgresql()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var userId = new UserId(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var titleId1 = new TitleId(Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var titleId2 = new TitleId(Guid.Parse("33333333-3333-4333-8333-333333333333"));
        var saved1 = SavedTitle.Create(userId, titleId1, OccurredAt);
        var saved2 = SavedTitle.Create(userId, titleId2, OccurredAt.AddMinutes(5));

        await using (var writeContext = CreateContext(database.ConnectionString))
        {
            await writeContext.Database.MigrateAsync();
            var repo = new EfSavedTitleRepository(writeContext);
            repo.Add(saved1);
            repo.Add(saved2);
            await repo.SaveChangesAsync(default);
        }

        await using var readContext = CreateContext(database.ConnectionString);
        var repoRead = new EfSavedTitleRepository(readContext);
        var materialized = await repoRead.FindAsync(userId, titleId1, default);
        materialized.Should().NotBeNull();
        materialized!.UserId.Should().Be(userId);
        materialized.TitleId.Should().Be(titleId1);
        materialized.SavedAt.Should().Be(OccurredAt);

        var queries = new EfUserLibraryQueries(readContext);
        var list = await queries.ListSavedAsync(userId, default);
        list.Should().HaveCount(2);
        list[0].TitleId.Should().Be(titleId2.Value);
        list[1].TitleId.Should().Be(titleId1.Value);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task WatchProgress_repository_handles_multiple_episodes_and_updates()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var userId = new UserId(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var titleId = new TitleId(Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var playableEp1 = new PlayableId(Guid.Parse("44444444-4444-4444-8444-444444444441"));
        var playableEp2 = new PlayableId(Guid.Parse("44444444-4444-4444-8444-444444444442"));

        await using (var writeContext = CreateContext(database.ConnectionString))
        {
            await writeContext.Database.MigrateAsync();
            var repo = new EfWatchProgressRepository(writeContext);
            repo.Add(WatchProgress.Record(userId, playableEp1, titleId, 1, WatchPosition.FromSeconds(100), OccurredAt));
            repo.Add(WatchProgress.Record(userId, playableEp2, titleId, 2, WatchPosition.FromSeconds(50), OccurredAt.AddMinutes(10)));
            await repo.SaveChangesAsync(default);
        }

        await using (var updateContext = CreateContext(database.ConnectionString))
        {
            var repo = new EfWatchProgressRepository(updateContext);
            var ep1 = await repo.FindAsync(userId, playableEp1, default);
            ep1.Should().NotBeNull();
            ep1!.UpdateProgress(1, WatchPosition.FromSeconds(250), OccurredAt.AddMinutes(20));
            await repo.SaveChangesAsync(default);
        }

        await using var verifyContext = CreateContext(database.ConnectionString);
        var queries = new EfUserLibraryQueries(verifyContext);
        var history = await queries.ListHistoryAsync(userId, default);

        history.Should().ContainSingle();
        history[0].TitleId.Should().Be(titleId.Value);
        history[0].PlayableId.Should().Be(playableEp1.Value);
        history[0].EpisodeNumber.Should().Be(1);
        history[0].ProgressSeconds.Should().Be(250);
        history[0].UpdatedAt.Should().Be(OccurredAt.AddMinutes(20));
    }

    private static EngagementDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EngagementDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new EngagementDbContext(options);
    }

    private static void AssertProperty(
        IEntityType entity,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        Type modelType,
        Type providerType,
        bool nullable,
        int? maximumLength)
    {
        var property = entity.FindProperty(propertyName);
        property.Should().NotBeNull();
        property!.GetColumnName(table).Should().Be(columnName);
        property.ClrType.Should().Be(modelType);
        property.IsNullable.Should().Be(nullable);
        property.GetMaxLength().Should().Be(maximumLength);
        (property.GetValueConverter()?.ProviderClrType ?? property.ClrType).Should().Be(providerType);
    }
}
