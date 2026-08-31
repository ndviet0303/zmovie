using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Personalization;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Personalization;
using ZMovie.Infrastructure.Personalization.Persistence;
using Xunit;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using PersonalizationTitleId = ZMovie.Domain.Personalization.TitleId;
using PersonalizationUserId = ZMovie.Domain.Personalization.UserId;
using PersonalizationRecId = ZMovie.Domain.Personalization.RecommendationId;
using PersonalizationEventId = ZMovie.Domain.Personalization.LearningEventId;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class PersonalizationPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Ef_mapping_matches_the_assistant_learning_events_schema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreatePersonalizationContext(database.ConnectionString);
        await context.Database.MigrateAsync();

        var entity = context.Model.FindEntityType(typeof(AssistantLearningEvent));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("assistant_learning_events");

        var tables = await ReadTablesAsync(database.ConnectionString);
        tables.Should().Contain("assistant_learning_events");
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task AssistantLearningEvent_roundtrip_materializes_private_state_and_typed_ids()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var title = MakeTitle("roundtrip-title", "Roundtrip Title", "Tựa Vòng Lặp");
            catalogContext.Titles.Add(title);
            await catalogContext.SaveChangesAsync();
        }

        var eventId = PersonalizationEventId.New();
        var recId = PersonalizationRecId.New();
        var userId = PersonalizationUserId.New();
        var titleId = new PersonalizationTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));

        var impression = AssistantLearningEvent.RecordImpression(
            eventId,
            recId,
            userId,
            titleId,
            "feature1,feature2",
            1,
            Timestamp);

        await using (var personalizationContext = CreatePersonalizationContext(database.ConnectionString))
        {
            await personalizationContext.Database.MigrateAsync();
            personalizationContext.AssistantLearningEvents.Add(impression);
            await personalizationContext.SaveChangesAsync();
        }

        await using var verifyContext = CreatePersonalizationContext(database.ConnectionString);
        var loaded = await verifyContext.AssistantLearningEvents.FirstOrDefaultAsync(x => x.Id == eventId);
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(eventId);
        loaded.RecommendationId.Should().Be(recId);
        loaded.UserId.Should().Be(userId);
        loaded.TitleId.Should().Be(titleId);
        loaded.Features.Should().Be("feature1,feature2");
        loaded.Rank.Should().Be(1);
        loaded.EventType.Should().Be("impression");
        loaded.Reward.Should().Be(0.0);
        loaded.CreatedAt.Should().Be(Timestamp);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Impression_and_feedback_lifecycle_and_ownership()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var titleA = MakeTitle("title-alpha", "Title Alpha", "Tựa Alpha", Guid.Parse("10000000-0000-4000-8000-000000000001"));
            var titleB = MakeTitle("title-beta", "Title Beta", "Tựa Beta", Guid.Parse("10000000-0000-4000-8000-000000000002"));
            catalogContext.Titles.AddRange(titleA, titleB);
            await catalogContext.SaveChangesAsync();
        }

        await using var context = CreatePersonalizationContext(database.ConnectionString);
        await context.Database.MigrateAsync();
        var repo = new EfPersonalizationLearningRepository(context);
        var user1 = PersonalizationUserId.New();
        var user2 = PersonalizationUserId.New();
        var recId = PersonalizationRecId.New();
        var titleAId = new PersonalizationTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var titleBId = new PersonalizationTitleId(Guid.Parse("10000000-0000-4000-8000-000000000002"));

        // Record impression for User 1 with Title A (rank 1) and Title B (rank 2)
        var impressionA = AssistantLearningEvent.RecordImpression(PersonalizationEventId.New(), recId, user1, titleAId, "drama,action", 1, Timestamp);
        var impressionB = AssistantLearningEvent.RecordImpression(PersonalizationEventId.New(), recId, user1, titleBId, "drama,action", 2, Timestamp);
        repo.AddRange([impressionA, impressionB]);
        await repo.SaveChangesAsync(default);

        // 1. User 1 finds existing impression for Title A
        var foundImpression = await repo.FindLatestImpressionAsync(user1, recId, titleAId, default);
        foundImpression.Should().NotBeNull();
        foundImpression!.Rank.Should().Be(1);

        // 2. User 2 cannot find impression (ownership isolation)
        var missingForOtherUser = await repo.FindLatestImpressionAsync(user2, recId, titleAId, default);
        missingForOtherUser.Should().BeNull();

        // 3. User 1 records feedback (like: reward 4.0)
        var feedback = AssistantLearningEvent.RecordFeedback(
            PersonalizationEventId.New(),
            recId,
            user1,
            titleAId,
            foundImpression.Features,
            foundImpression.Rank,
            FeedbackEventType.Like,
            RewardPolicy.GetReward(FeedbackEventType.Like),
            Timestamp.AddMinutes(5));
        repo.Add(feedback);
        await repo.SaveChangesAsync(default);

        await using var verifyContext = CreatePersonalizationContext(database.ConnectionString);
        var events = await verifyContext.AssistantLearningEvents.Where(x => x.UserId == user1).ToListAsync();
        events.Should().HaveCount(3);
        events.Count(x => x.EventType == "impression").Should().Be(2);
        events.Count(x => x.EventType == "like").Should().Be(1);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Score_decay_over_time_and_180_day_retention_cutoff()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using (var catalogContext = CreateCatalogContext(database.ConnectionString))
        {
            await catalogContext.Database.MigrateAsync();
            var titleRecent = MakeTitle("title-recent", "Title Recent", "Tựa Gần Đây", Guid.Parse("10000000-0000-4000-8000-000000000001"));
            var titleOld = MakeTitle("title-old", "Title Old", "Tựa Cũ", Guid.Parse("10000000-0000-4000-8000-000000000002"));
            var titleExpired = MakeTitle("title-expired", "Title Expired", "Tựa Hết Hạn", Guid.Parse("10000000-0000-4000-8000-000000000003"));
            catalogContext.Titles.AddRange(titleRecent, titleOld, titleExpired);
            await catalogContext.SaveChangesAsync();
        }

        var userId = PersonalizationUserId.New();
        var recId = PersonalizationRecId.New();
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

        var token = "magic";
        var feature = EfPersonalizationQueries.HashFeature(token);

        var titleRecentId = new PersonalizationTitleId(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        var titleOldId = new PersonalizationTitleId(Guid.Parse("10000000-0000-4000-8000-000000000002"));
        var titleExpiredId = new PersonalizationTitleId(Guid.Parse("10000000-0000-4000-8000-000000000003"));

        // 1. Recent feedback (today, decay ~= 1.0, reward 4.0 (like), weight 1) -> score ~= 4.0
        var recentEvent = AssistantLearningEvent.RecordFeedback(
            PersonalizationEventId.New(),
            recId,
            userId,
            titleRecentId,
            feature,
            1,
            FeedbackEventType.Like,
            4.0,
            now);

        // 2. 45-day-old feedback (decay = e^(-45/45) = e^-1 ~= 0.367879, reward 4.0, weight 1) -> score ~= 1.4715
        var oldEvent = AssistantLearningEvent.RecordFeedback(
            PersonalizationEventId.New(),
            recId,
            userId,
            titleOldId,
            feature,
            1,
            FeedbackEventType.Like,
            4.0,
            now.AddDays(-45));

        // 3. 185-day-old feedback (older than 180 days -> excluded by cutoff)
        var expiredEvent = AssistantLearningEvent.RecordFeedback(
            PersonalizationEventId.New(),
            recId,
            userId,
            titleExpiredId,
            feature,
            1,
            FeedbackEventType.Like,
            4.0,
            now.AddDays(-185));

        await using (var personalizationContext = CreatePersonalizationContext(database.ConnectionString))
        {
            await personalizationContext.Database.MigrateAsync();
            personalizationContext.AssistantLearningEvents.AddRange(recentEvent, oldEvent, expiredEvent);
            await personalizationContext.SaveChangesAsync();
        }

        await using var readContext = CreatePersonalizationContext(database.ConnectionString);
        var queries = new EfPersonalizationQueries(readContext, NullLogger<EfPersonalizationQueries>.Instance);
        var tokens = new Dictionary<string, int> { [token] = 1 };

        var scores = await queries.GetTitleScoresAsync(userId, tokens, now, default);

        scores.Should().ContainKey(titleRecentId.Value);
        scores[titleRecentId.Value].Should().BeApproximately(4.0, 0.05);

        scores.Should().ContainKey(titleOldId.Value);
        scores[titleOldId.Value].Should().BeApproximately(4.0 * Math.Exp(-1), 0.05);

        scores.Should().NotContainKey(titleExpiredId.Value, "Events older than 180 days must be excluded from scoring.");
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

    private static PersonalizationDbContext CreatePersonalizationContext(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<PersonalizationDbContext>();
        builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        return new PersonalizationDbContext(builder.Options);
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
