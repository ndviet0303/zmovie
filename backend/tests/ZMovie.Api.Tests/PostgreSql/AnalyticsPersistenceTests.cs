using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Analytics;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Analytics;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using Xunit;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;
using AnalyticsUserId = ZMovie.Domain.Analytics.UserId;
using AnalyticsViewEventId = ZMovie.Domain.Analytics.ViewEventId;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class AnalyticsPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Ef_mapping_matches_the_title_view_events_schema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreateAnalyticsContext(database.ConnectionString);
        await context.Database.MigrateAsync();

        var entity = context.Model.FindEntityType(typeof(TitleViewEvent));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("title_view_events");

        var tables = await ReadTablesAsync(database.ConnectionString);
        tables.Should().Contain("title_view_events");
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task TitleViewEvent_roundtrip_materializes_private_state_and_typed_ids()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var title = MakeTitle("title-one", "Title One", "Tựa Một");
            catalogContext.Titles.Add(title);
            await catalogContext.SaveChangesAsync();
        }

        var id = AnalyticsViewEventId.New();
        var titleId = new AnalyticsTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var userId = AnalyticsUserId.New();

        await using (var analyticsContext = CreateAnalyticsContext(database.ConnectionString))
        {
            await analyticsContext.Database.MigrateAsync();
            var viewEvent = TitleViewEvent.Record(id, titleId, 3, userId, "session-1", Timestamp);
            analyticsContext.TitleViewEvents.Add(viewEvent);
            await analyticsContext.SaveChangesAsync();
        }

        await using var verifyContext = CreateAnalyticsContext(database.ConnectionString);
        var loaded = await verifyContext.TitleViewEvents.FirstOrDefaultAsync(x => x.Id == id);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(id);
        loaded.TitleId.Should().Be(titleId);
        loaded.EpisodeNumber.Should().Be(3);
        loaded.UserId.Should().Be(userId);
        loaded.SessionId.Should().Be("session-1");
        loaded.ViewedAt.Should().Be(Timestamp);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Concurrent_duplicate_views_under_advisory_lock_count_exactly_once()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var title = MakeTitle("concurrent-title", "Concurrent Title", "Tựa Đồng Thời");
            catalogContext.Titles.Add(title);
            await catalogContext.SaveChangesAsync();
        }

        await using (var migrateAnalytics = CreateAnalyticsContext(database.ConnectionString))
        {
            await migrateAnalytics.Database.MigrateAsync();
        }

        var titleId = new AnalyticsTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var userId = AnalyticsUserId.New();
        const string session = "concurrent-session";

        // Launch 10 parallel view recordings at the exact same timestamp
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = CreateAnalyticsContext(database.ConnectionString);
            var repo = new EfViewEventRepository(context);
            return await repo.RecordViewWithLockAsync(titleId, 1, userId, session, Timestamp, default);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        var countedTrue = results.Count(r => r.Counted);
        var countedFalse = results.Count(r => !r.Counted);

        countedTrue.Should().Be(1, "Exactly one concurrent attempt should be counted as a new view.");
        countedFalse.Should().Be(9, "The other 9 concurrent attempts within 30m window should be deduplicated.");

        await using var verifyContext = CreateAnalyticsContext(database.ConnectionString);
        var totalPersisted = await verifyContext.TitleViewEvents.CountAsync(x => x.TitleId == titleId);
        totalPersisted.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Anonymous_versus_authenticated_identity_deduplication()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var title = MakeTitle("dedupe-title", "Dedupe Title", "Tựa Khử Trùng");
            catalogContext.Titles.Add(title);
            await catalogContext.SaveChangesAsync();
        }

        await using var context = CreateAnalyticsContext(database.ConnectionString);
        await context.Database.MigrateAsync();
        var repo = new EfViewEventRepository(context);
        var titleId = new AnalyticsTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var user1 = AnalyticsUserId.New();
        var user2 = AnalyticsUserId.New();
        const string sharedSession = "shared-browser-session";

        // 1. Authenticated user 1 records view
        var r1 = await repo.RecordViewWithLockAsync(titleId, null, user1, sharedSession, Timestamp, default);
        r1.Counted.Should().BeTrue();

        // 2. Authenticated user 2 records view from same browser session -> counted because user IDs differ
        var r2 = await repo.RecordViewWithLockAsync(titleId, null, user2, sharedSession, Timestamp, default);
        r2.Counted.Should().BeTrue();

        // 3. User 1 records duplicate view 10 minutes later -> deduplicated
        var r3 = await repo.RecordViewWithLockAsync(titleId, null, user1, sharedSession, Timestamp.AddMinutes(10), default);
        r3.Counted.Should().BeFalse();

        // 4. Anonymous user records view -> counted because userId is null
        var r4 = await repo.RecordViewWithLockAsync(titleId, null, null, sharedSession, Timestamp, default);
        r4.Counted.Should().BeTrue();

        // 5. Anonymous duplicate view from same session -> deduplicated
        var r5 = await repo.RecordViewWithLockAsync(titleId, null, null, sharedSession, Timestamp.AddMinutes(15), default);
        r5.Counted.Should().BeFalse();

        // 6. Anonymous view from different session -> counted
        var r6 = await repo.RecordViewWithLockAsync(titleId, null, null, "different-session", Timestamp.AddMinutes(15), default);
        r6.Counted.Should().BeTrue();

        // 7. User 1 records view 31 minutes later (outside 30m dedupe window) -> counted
        var r7 = await repo.RecordViewWithLockAsync(titleId, null, user1, sharedSession, Timestamp.AddMinutes(31), default);
        r7.Counted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Day_week_month_boundaries_in_Asia_Ho_Chi_Minh_and_top_title_ordering()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var titleA = MakeTitle("title-a", "Title A", "Tựa A", Guid.Parse("10000000-0000-4000-8000-000000000001"));
            var titleB = MakeTitle("title-b", "Title B", "Tựa B", Guid.Parse("10000000-0000-4000-8000-000000000002"));
            var titleC = MakeTitle("title-c", "Title C", "Tựa C", Guid.Parse("10000000-0000-4000-8000-000000000003"));
            catalogContext.Titles.AddRange(titleA, titleB, titleC);
            await catalogContext.SaveChangesAsync();
        }

        await using var context = CreateAnalyticsContext(database.ConnectionString);
        await context.Database.MigrateAsync();
        var titleAId = new AnalyticsTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var titleBId = new AnalyticsTitleId(Guid.Parse("10000000-0000-4000-8000-000000000002"));
        var titleCId = new AnalyticsTitleId(Guid.Parse("10000000-0000-4000-8000-000000000003"));

        // Baseline: Monday 2026-08-31 12:00:00 UTC = 19:00:00 ICT
        var nowUtc = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

        // Views today (2026-08-31 ICT):
        context.TitleViewEvents.AddRange(
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleAId, null, null, "s1", nowUtc.AddHours(-1)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleAId, null, null, "s2", nowUtc.AddHours(-2)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleAId, null, null, "s3", nowUtc.AddHours(-3)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleBId, null, null, "s4", nowUtc.AddHours(-1)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleBId, null, null, "s5", nowUtc.AddHours(-2)));

        // Views yesterday (2026-08-30 ICT):
        context.TitleViewEvents.AddRange(
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleBId, null, null, "s6", nowUtc.AddDays(-1)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleBId, null, null, "s7", nowUtc.AddDays(-1)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleCId, null, null, "s8", nowUtc.AddDays(-1)));

        // Views earlier in the month (2026-08-10 ICT):
        context.TitleViewEvents.AddRange(
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleCId, null, null, "s9", new DateTimeOffset(2026, 8, 10, 5, 0, 0, TimeSpan.Zero)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleCId, null, null, "s10", new DateTimeOffset(2026, 8, 10, 6, 0, 0, TimeSpan.Zero)),
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleCId, null, null, "s11", new DateTimeOffset(2026, 8, 10, 7, 0, 0, TimeSpan.Zero)));

        // Views previous month (2026-07-31 ICT):
        context.TitleViewEvents.Add(
            TitleViewEvent.Record(AnalyticsViewEventId.New(), titleAId, null, null, "s12", new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero)));

        await context.SaveChangesAsync();

        var queries = new EfViewAnalyticsQueries(context);

        // 1. Day period: Title A has 3 views, Title B has 2 views
        var dayTop = await queries.GetTopAsync(TopPeriod.Day, 10, nowUtc, default);
        dayTop.Select(x => (x.TitleId, x.Views)).Should().Equal(
            (titleAId.Value, 3L),
            (titleBId.Value, 2L));

        // 2. Week period (current week starts Monday 2026-08-31 00:00:00 ICT = 2026-08-30 17:00:00 UTC)
        var weekTop = await queries.GetTopAsync(TopPeriod.Week, 10, nowUtc, default);
        weekTop.Select(x => (x.TitleId, x.Views)).Should().Equal(
            (titleAId.Value, 3L),
            (titleBId.Value, 2L));

        // 3. Month period (month starts 2026-08-01 00:00:00 ICT = 2026-07-31 17:00:00 UTC)
        // Title A: 3 views, Title B: 4 views, Title C: 4 views
        var monthTop = await queries.GetTopAsync(TopPeriod.Month, 10, nowUtc, default);
        monthTop.Select(x => x.Views).Should().Equal(4L, 4L, 3L);
        monthTop.First().Views.Should().Be(4L);
    }

    private static Title MakeTitle(string slug, string english, string vietnamese, Guid? id = null) =>
        Title.Create(
            new CatalogTitleId(id ?? Guid.Parse("10000000-0000-4000-8000-000000000001")),
            TitleSlug.Parse(slug),
            new LocalizedText(vietnamese, english),
            new LocalizedText(string.Empty, string.Empty),
            "Drama",
            ReleaseYear.FromInt(2026),
            TitleType.Movie,
            "poster",
            Runtime.FromMinutes(90),
            false,
            Timestamp);

    private static AnalyticsDbContext CreateAnalyticsContext(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AnalyticsDbContext>();
        builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        return new AnalyticsDbContext(builder.Options);
    }

    private static CatalogDbContext CreateCatalogContext(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<CatalogDbContext>();
        builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        return new CatalogDbContext(builder.Options);
    }

    private static async Task<IReadOnlyList<string>> ReadTablesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            ORDER BY table_name;
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}
