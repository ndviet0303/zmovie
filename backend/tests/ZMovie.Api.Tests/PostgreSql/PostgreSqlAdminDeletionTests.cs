using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Administration;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Analytics;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Common;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization;
using ZMovie.Infrastructure.Personalization.Persistence;
using Xunit;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;
using EngagementUserId = ZMovie.Domain.Engagement.UserId;
using PersonalizationUserId = ZMovie.Domain.Personalization.UserId;
using PersonalizationTitleId = ZMovie.Domain.Personalization.TitleId;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgreSqlAdminDeletionTests(PostgreSqlFixture fixture)
{
    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Hard_deletion_removes_every_dependent_row_across_contexts_on_real_postgresql()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var titleId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var survivorId = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var userId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var now = DateTimeOffset.UtcNow;

        await MigrateAllAsync(database.ConnectionString);

        // Seed Catalog
        await using (var catalog = CreateCatalogContext(database.ConnectionString))
        {
            var title = Title.Create(
                new CatalogTitleId(titleId),
                TitleSlug.Parse("doomed-title"),
                new LocalizedText("Phim Xoá", "Doomed"),
                new LocalizedText("Mô tả", "Synopsis"),
                "Action",
                ReleaseYear.FromInt(2025),
                TitleType.Series,
                "https://cdn/p.jpg",
                Runtime.FromMinutes(90),
                false,
                now);
            var survivor = Title.Create(
                new CatalogTitleId(survivorId),
                TitleSlug.Parse("survivor-title"),
                new LocalizedText("Phim Sống", "Survivor"),
                new LocalizedText("Mô tả 2", "Synopsis 2"),
                "Drama",
                ReleaseYear.FromInt(2026),
                TitleType.Movie,
                "https://cdn/s.jpg",
                Runtime.FromMinutes(120),
                true,
                now);

            catalog.Titles.AddRange(title, survivor);
            catalog.Episodes.Add(Episode.Create(EpisodeId.New(), new CatalogTitleId(titleId), 1, "Tập 1", "https://video/1"));
            var genre = Genre.Create(GenreId.New(), "action", "Action", now);
            catalog.Genres.Add(genre);
            catalog.TitleGenres.Add(TitleGenreAssignment.Create(new CatalogTitleId(titleId), genre.Id, now));
            await catalog.SaveChangesAsync();
        }

        // Seed Engagement
        await using (var engagement = CreateEngagementContext(database.ConnectionString))
        {
            engagement.SavedTitles.AddRange(
                SavedTitle.Create(new EngagementUserId(userId), new EngagementTitleId(titleId), now),
                SavedTitle.Create(new EngagementUserId(userId), new EngagementTitleId(survivorId), now));
            engagement.WatchHistory.Add(WatchProgress.Record(
                new EngagementUserId(userId),
                new ZMovie.Domain.Engagement.PlayableId(Guid.NewGuid()),
                new EngagementTitleId(titleId),
                1,
                WatchPosition.FromSeconds(50),
                now));
            engagement.TitleReviews.Add(Review.Create(
                ReviewId.New(),
                new EngagementTitleId(titleId),
                new EngagementUserId(userId),
                "Reviewer",
                9,
                "Great",
                now).Review!);
            await engagement.SaveChangesAsync();
        }

        // Seed Analytics
        await using (var analytics = CreateAnalyticsContext(database.ConnectionString))
        {
            analytics.TitleViewEvents.Add(TitleViewEvent.Record(
                ViewEventId.New(),
                new AnalyticsTitleId(titleId),
                1,
                null,
                "session-1",
                now));
            await analytics.SaveChangesAsync();
        }

        // Seed Personalization
        await using (var personalization = CreatePersonalizationContext(database.ConnectionString))
        {
            personalization.AssistantLearningEvents.Add(AssistantLearningEvent.RecordImpression(
                LearningEventId.New(),
                RecommendationId.New(),
                new PersonalizationUserId(userId),
                new PersonalizationTitleId(titleId),
                "f1",
                1,
                now));
            await personalization.SaveChangesAsync();
        }

        // Execute deletion
        await using (var catalog = CreateCatalogContext(database.ConnectionString))
        await using (var identity = CreateIdentityContext(database.ConnectionString))
        await using (var engagement = CreateEngagementContext(database.ConnectionString))
        await using (var analytics = CreateAnalyticsContext(database.ConnectionString))
        await using (var personalization = CreatePersonalizationContext(database.ConnectionString))
        {
            var coordinator = new AdminTitleDeletionCoordinator(
                new EfCatalogTitleCleanupPort(catalog),
                new EfEngagementTitleCleanupPort(engagement),
                new EfAnalyticsTitleCleanupPort(analytics),
                new EfPersonalizationTitleCleanupPort(personalization),
                new NpgsqlTransactionCoordinator(catalog, identity, engagement, analytics, personalization));

            var result = await coordinator.DeleteTitleAsync("doomed-title", default);
            result.Should().BeTrue();

            var missing = await coordinator.DeleteTitleAsync("doomed-title", default);
            missing.Should().BeFalse();
        }

        // Verify across all contexts
        await using (var catalog = CreateCatalogContext(database.ConnectionString))
        {
            (await catalog.Titles.AnyAsync(x => x.Id == new CatalogTitleId(titleId))).Should().BeFalse();
            (await catalog.Episodes.AnyAsync(x => x.TitleId == new CatalogTitleId(titleId))).Should().BeFalse();
            (await catalog.TitleGenres.AnyAsync(x => x.TitleId == new CatalogTitleId(titleId))).Should().BeFalse();
            (await catalog.Titles.AnyAsync(x => x.Id == new CatalogTitleId(survivorId))).Should().BeTrue();
        }

        await using (var engagement = CreateEngagementContext(database.ConnectionString))
        {
            (await engagement.WatchHistory.AnyAsync(x => x.TitleId == new EngagementTitleId(titleId))).Should().BeFalse();
            (await engagement.TitleReviews.AnyAsync(x => x.TitleId == new EngagementTitleId(titleId))).Should().BeFalse();
            (await engagement.SavedTitles.AnyAsync(x => x.TitleId == new EngagementTitleId(titleId))).Should().BeFalse();
            (await engagement.SavedTitles.AnyAsync(x => x.TitleId == new EngagementTitleId(survivorId))).Should().BeTrue();
        }

        await using (var analytics = CreateAnalyticsContext(database.ConnectionString))
        {
            (await analytics.TitleViewEvents.AnyAsync(x => x.TitleId == new AnalyticsTitleId(titleId))).Should().BeFalse();
        }

        await using (var personalization = CreatePersonalizationContext(database.ConnectionString))
        {
            (await personalization.AssistantLearningEvents.AnyAsync(x => x.TitleId == new PersonalizationTitleId(titleId))).Should().BeFalse();
        }
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Hard_deletion_is_atomic_and_rolls_back_if_coordinator_fails()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var titleId = Guid.Parse("44444444-4444-4444-8444-444444444444");
        var now = DateTimeOffset.UtcNow;

        await MigrateAllAsync(database.ConnectionString);

        await using (var catalog = CreateCatalogContext(database.ConnectionString))
        {
            var title = Title.Create(
                new CatalogTitleId(titleId),
                TitleSlug.Parse("failing-title"),
                new LocalizedText("Phim Lỗi", "Failing"),
                new LocalizedText("Mô tả", "Synopsis"),
                "Drama",
                ReleaseYear.FromInt(2025),
                TitleType.Movie,
                "https://cdn/f.jpg",
                Runtime.FromMinutes(100),
                false,
                now);
            catalog.Titles.Add(title);
            await catalog.SaveChangesAsync();
        }

        await using (var engagement = CreateEngagementContext(database.ConnectionString))
        {
            engagement.TitleReviews.Add(Review.Create(
                ReviewId.New(),
                new EngagementTitleId(titleId),
                new EngagementUserId(Guid.NewGuid()),
                "Reviewer",
                10,
                null,
                now).Review!);
            await engagement.SaveChangesAsync();
        }

        await using (var catalog = CreateCatalogContext(database.ConnectionString))
        await using (var identity = CreateIdentityContext(database.ConnectionString))
        await using (var engagement = CreateEngagementContext(database.ConnectionString))
        await using (var analytics = CreateAnalyticsContext(database.ConnectionString))
        await using (var personalization = CreatePersonalizationContext(database.ConnectionString))
        {
            var failingCoordinator = new AdminTitleDeletionCoordinator(
                new FailingCatalogCleanupPort(),
                new EfEngagementTitleCleanupPort(engagement),
                new EfAnalyticsTitleCleanupPort(analytics),
                new EfPersonalizationTitleCleanupPort(personalization),
                new NpgsqlTransactionCoordinator(catalog, identity, engagement, analytics, personalization));

            var act = () => failingCoordinator.DeleteTitleAsync("failing-title", default);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // Verify that rollback preserved the engagement review and catalog title
        await using (var catalog = CreateCatalogContext(database.ConnectionString))
        {
            (await catalog.Titles.AnyAsync(x => x.Id == new CatalogTitleId(titleId))).Should().BeTrue();
        }

        await using (var engagement = CreateEngagementContext(database.ConnectionString))
        {
            (await engagement.TitleReviews.AnyAsync(x => x.TitleId == new EngagementTitleId(titleId))).Should().BeTrue();
        }
    }

    private sealed class FailingCatalogCleanupPort : ICatalogTitleCleanupPort
    {
        public Task<Title?> FindBySlugAsync(string slug, CancellationToken ct) =>
            Task.FromResult<Title?>(Title.Create(
                new CatalogTitleId(Guid.Parse("44444444-4444-4444-8444-444444444444")),
                TitleSlug.Parse(slug),
                new LocalizedText("Vietnamese", "English"),
                new LocalizedText("Syn", "Syn"),
                "Drama",
                ReleaseYear.FromInt(2025),
                TitleType.Movie,
                "https://cdn/p.jpg",
                Runtime.FromMinutes(90),
                false,
                DateTimeOffset.UtcNow));

        public Task DeleteTitleAggregateAsync(CatalogTitleId titleId, CancellationToken ct) =>
            throw new InvalidOperationException("Simulated catalog failure for rollback testing.");
    }

    private static async Task MigrateAllAsync(string connectionString)
    {
        await using var c = CreateCatalogContext(connectionString);
        await using var i = CreateIdentityContext(connectionString);
        await using var e = CreateEngagementContext(connectionString);
        await using var a = CreateAnalyticsContext(connectionString);
        await using var p = CreatePersonalizationContext(connectionString);

        await c.Database.MigrateAsync();
        await i.Database.MigrateAsync();
        await e.Database.MigrateAsync();
        await a.Database.MigrateAsync();
        await p.Database.MigrateAsync();
    }

    private static CatalogDbContext CreateCatalogContext(string cs) =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(cs, x => x.MigrationsHistoryTable("__ef_migrations_history_catalog", "public")).UseSnakeCaseNamingConvention().Options);

    private static IdentityDbContext CreateIdentityContext(string cs) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(cs, x => x.MigrationsHistoryTable("__ef_migrations_history_identity", "public")).UseSnakeCaseNamingConvention().Options);

    private static EngagementDbContext CreateEngagementContext(string cs) =>
        new(new DbContextOptionsBuilder<EngagementDbContext>().UseNpgsql(cs, x => x.MigrationsHistoryTable("__ef_migrations_history_engagement", "public")).UseSnakeCaseNamingConvention().Options);

    private static AnalyticsDbContext CreateAnalyticsContext(string cs) =>
        new(new DbContextOptionsBuilder<AnalyticsDbContext>().UseNpgsql(cs, x => x.MigrationsHistoryTable("__ef_migrations_history_analytics", "public")).UseSnakeCaseNamingConvention().Options);

    private static PersonalizationDbContext CreatePersonalizationContext(string cs) =>
        new(new DbContextOptionsBuilder<PersonalizationDbContext>().UseNpgsql(cs, x => x.MigrationsHistoryTable("__ef_migrations_history_personalization", "public")).UseSnakeCaseNamingConvention().Options);
}
