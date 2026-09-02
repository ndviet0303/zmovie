using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Administration;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Administration;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using Xunit;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class ReviewPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 31, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Ef_mapping_matches_the_existing_title_reviews_schema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.MigrateAsync();

        var review = context.Model.FindEntityType(typeof(Review));
        review.Should().NotBeNull();
        review!.GetTableName().Should().Be("title_reviews");
        review.GetSchema().Should().Be("public");

        var table = StoreObjectIdentifier.Table("title_reviews", "public");
        review.FindPrimaryKey()!.Properties.Select(property => property.Name)
            .Should().Equal(nameof(Review.Id));
        review.FindPrimaryKey()!.GetName(table).Should().Be("pk_title_reviews");

        var indexes = review.GetIndexes().ToDictionary(index => index.GetDatabaseName()!);
        indexes["ix_title_reviews_title_id_user_id"].IsUnique.Should().BeTrue();
        indexes["ix_title_reviews_title_id_user_id"].Properties.Select(property => property.Name)
            .Should().Equal(nameof(Review.TitleId), nameof(Review.UserId));
        indexes["ix_title_reviews_title_id_updated_at"].IsUnique.Should().BeFalse();
        indexes["ix_title_reviews_title_id_updated_at"].Properties.Select(property => property.Name)
            .Should().Equal(nameof(Review.TitleId), nameof(Review.UpdatedAt));

        AssertProperty(review, table, nameof(Review.Id), "id", typeof(ReviewId), typeof(Guid), false, null);
        AssertProperty(review, table, nameof(Review.TitleId), "title_id", typeof(EngagementTitleId), typeof(Guid), false, null);
        AssertProperty(review, table, nameof(Review.UserId), "user_id", typeof(UserId), typeof(Guid), false, null);
        AssertProperty(review, table, nameof(Review.AuthorName), "author_name", typeof(string), typeof(string), false, Review.MaximumAuthorNameLength);
        AssertProperty(review, table, nameof(Review.Rating), "rating", typeof(Rating), typeof(int), false, null);
        AssertProperty(review, table, nameof(Review.Comment), "comment", typeof(string), typeof(string), true, Review.MaximumCommentLength);
        AssertProperty(review, table, nameof(Review.CreatedAt), "created_at", typeof(DateTimeOffset), typeof(DateTimeOffset), false, null);
        AssertProperty(review, table, nameof(Review.UpdatedAt), "updated_at", typeof(DateTimeOffset), typeof(DateTimeOffset), false, null);

        (await ReadColumnsAsync(database.ConnectionString)).Should().Equal(
            new ColumnDefinition("id", "uuid", false, null),
            new ColumnDefinition("title_id", "uuid", false, null),
            new ColumnDefinition("user_id", "uuid", false, null),
            new ColumnDefinition("author_name", "character varying", false, Review.MaximumAuthorNameLength),
            new ColumnDefinition("rating", "integer", false, null),
            new ColumnDefinition("comment", "character varying", true, Review.MaximumCommentLength),
            new ColumnDefinition("created_at", "timestamp with time zone", false, null),
            new ColumnDefinition("updated_at", "timestamp with time zone", false, null));

        var databaseIndexes = await ReadIndexesAsync(database.ConnectionString);
        databaseIndexes["pk_title_reviews"].Should().Contain("UNIQUE INDEX").And.Contain("(id)");
        databaseIndexes["ix_title_reviews_title_id_user_id"]
            .Should().Contain("UNIQUE INDEX").And.Contain("(title_id, user_id)");
        databaseIndexes["ix_title_reviews_title_id_updated_at"]
            .Should().NotContain("UNIQUE INDEX").And.Contain("(title_id, updated_at)");
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Repository_materializes_private_state_typed_ids_rating_and_create_timestamps()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var reviewId = new ReviewId(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var titleId = new EngagementTitleId(Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var userId = new UserId(Guid.Parse("33333333-3333-4333-8333-333333333333"));
        var created = CreateReview(reviewId, titleId, userId, "Original author", 8, "  Original comment  ", CreatedAt);

        await using (var writeContext = CreateContext(database.ConnectionString))
        {
            await writeContext.Database.MigrateAsync();
            var repository = new EfReviewRepository(writeContext);
            repository.Add(created);
            await repository.SaveChangesAsync(default);
        }

        await using var readContext = CreateContext(database.ConnectionString);
        var materialized = await new EfReviewRepository(readContext)
            .FindByTitleAndUserAsync(titleId, userId, default);

        materialized.Should().NotBeNull();
        materialized!.Id.Should().Be(reviewId);
        materialized.TitleId.Should().Be(titleId);
        materialized.UserId.Should().Be(userId);
        materialized.AuthorName.Should().Be("Original author");
        materialized.Rating.Value.Should().Be(8);
        materialized.Comment.Should().Be("Original comment");
        materialized.CreatedAt.Should().Be(CreatedAt);
        materialized.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Repository_edit_preserves_created_at_and_updates_updated_at()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var reviewId = new ReviewId(Guid.Parse("44444444-4444-4444-8444-444444444444"));
        var titleId = new EngagementTitleId(Guid.Parse("55555555-5555-4555-8555-555555555555"));
        var userId = new UserId(Guid.Parse("66666666-6666-4666-8666-666666666666"));
        var editedAt = CreatedAt.AddHours(2);

        await using (var createContext = CreateContext(database.ConnectionString))
        {
            await createContext.Database.MigrateAsync();
            var repository = new EfReviewRepository(createContext);
            repository.Add(CreateReview(reviewId, titleId, userId, "Original author", 6, "Original", CreatedAt));
            await repository.SaveChangesAsync(default);
        }

        await using (var editContext = CreateContext(database.ConnectionString))
        {
            var repository = new EfReviewRepository(editContext);
            var review = await repository.FindByIdAsync(reviewId, default);
            review.Should().NotBeNull();

            var decision = review!.Edit("Updated author", 9, "  Updated comment  ", editedAt);
            decision.IsAccepted.Should().BeTrue();
            await repository.SaveChangesAsync(default);
        }

        await using var verifyContext = CreateContext(database.ConnectionString);
        var updated = await new EfReviewRepository(verifyContext).FindByIdAsync(reviewId, default);

        updated.Should().NotBeNull();
        updated!.CreatedAt.Should().Be(CreatedAt);
        updated.UpdatedAt.Should().Be(editedAt);
        updated.AuthorName.Should().Be("Updated author");
        updated.Rating.Value.Should().Be(9);
        updated.Comment.Should().Be("Updated comment");
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Review_queries_project_typed_values_order_and_filter_on_postgresql()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var titleId = new EngagementTitleId(Guid.Parse("77777777-7777-4777-8777-777777777777"));
        var otherTitleId = new EngagementTitleId(Guid.Parse("88888888-8888-4888-8888-888888888888"));
        var older = CreateReview(
            new ReviewId(Guid.Parse("10000000-0000-4000-8000-000000000001")),
            titleId,
            new UserId(Guid.Parse("20000000-0000-4000-8000-000000000001")),
            "Needle reviewer",
            3,
            "Needs work",
            CreatedAt.AddMinutes(1));
        var newer = CreateReview(
            new ReviewId(Guid.Parse("10000000-0000-4000-8000-000000000002")),
            titleId,
            new UserId(Guid.Parse("20000000-0000-4000-8000-000000000002")),
            "Enthusiastic reviewer",
            9,
            null,
            CreatedAt.AddMinutes(3));
        var other = CreateReview(
            new ReviewId(Guid.Parse("10000000-0000-4000-8000-000000000003")),
            otherTitleId,
            new UserId(Guid.Parse("20000000-0000-4000-8000-000000000003")),
            "Other reviewer",
            7,
            "Needle on another title",
            CreatedAt.AddMinutes(2));

        await using (var catalogWrite = CreateCatalogContext(database.ConnectionString))
        {
            await catalogWrite.Database.MigrateAsync();
            catalogWrite.Titles.AddRange(
                CreateTitle(titleId.Value, "typed-review-title", "Phim danh gia"),
                CreateTitle(otherTitleId.Value, "other-review-title", "Phim khac"));
            await catalogWrite.SaveChangesAsync();
        }

        await using (var writeContext = CreateEngagementContext(database.ConnectionString))
        {
            await writeContext.Database.MigrateAsync();
            writeContext.TitleReviews.AddRange(older, newer, other);
            await writeContext.SaveChangesAsync();
        }

        await using var readEngagement = CreateEngagementContext(database.ConnectionString);
        await using var readCatalog = CreateCatalogContext(database.ConnectionString);
        var reviews = await new EfReviewQueries(readEngagement).ListByTitleAsync(titleId, default);

        reviews.Should().Equal(
            new ReviewEntry(newer.Id.Value, newer.AuthorName, 9, null, newer.UpdatedAt),
            new ReviewEntry(older.Id.Value, older.AuthorName, 3, "Needs work", older.UpdatedAt));

        await using var readIdentity = CreateIdentityContext(database.ConnectionString);
        await using var readAnalytics = CreateAnalyticsContext(database.ConnectionString);
        await readIdentity.Database.MigrateAsync();
        await readAnalytics.Database.MigrateAsync();

        var admin = new EfAdminDashboardQueries(readCatalog, readIdentity, readEngagement, readAnalytics);
        var overview = await admin.GetOverviewAsync(default);
        overview.ReviewCount.Should().Be(3);
        overview.AverageRating.Should().Be(6.3);

        var filtered = await admin.ListReviewsAsync("needle", 3, 1, 20, default);
        filtered.Total.Should().Be(1);
        filtered.Items.Should().Equal(new AdminReviewSummary(
            older.Id.Value,
            "typed-review-title",
            "Phim danh gia",
            older.UserId.Value,
            "Needle reviewer",
            3,
            "Needs work",
            older.UpdatedAt));
    }

    private static Review CreateReview(
        ReviewId reviewId,
        EngagementTitleId titleId,
        UserId userId,
        string authorName,
        int rating,
        string? comment,
        DateTimeOffset occurredAt)
    {
        var decision = Review.Create(reviewId, titleId, userId, authorName, rating, comment, occurredAt);
        decision.IsAccepted.Should().BeTrue();
        return decision.Review!;
    }

    private static Title CreateTitle(Guid id, string slug, string vietnameseTitle) =>
        Title.Create(
            new CatalogTitleId(id),
            TitleSlug.Parse(slug),
            new LocalizedText(vietnameseTitle, slug),
            new LocalizedText("Mo ta", "Synopsis"),
            "Drama",
            ReleaseYear.FromInt(2026),
            TitleType.Movie,
            "https://cdn.example/poster.jpg",
            Runtime.FromMinutes(90),
            false,
            CreatedAt);

    private static EngagementDbContext CreateContext(string connectionString) => CreateEngagementContext(connectionString);

    private static EngagementDbContext CreateEngagementContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EngagementDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new EngagementDbContext(options);
    }

    private static CatalogDbContext CreateCatalogContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_catalog", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CatalogDbContext(options);
    }

    private static IdentityDbContext CreateIdentityContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_identity", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new IdentityDbContext(options);
    }

    private static AnalyticsDbContext CreateAnalyticsContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_analytics", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AnalyticsDbContext(options);
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

    private static async Task<IReadOnlyList<ColumnDefinition>> ReadColumnsAsync(string connectionString)
    {
        var columns = new List<ColumnDefinition>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT column_name, data_type, is_nullable, character_maximum_length
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'title_reviews'
            ORDER BY ordinal_position
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2) == "YES",
                reader.IsDBNull(3) ? null : reader.GetInt32(3)));
        }

        return columns;
    }

    private static async Task<IReadOnlyDictionary<string, string>> ReadIndexesAsync(string connectionString)
    {
        var indexes = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT indexname, indexdef
            FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'title_reviews'
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0), reader.GetString(1));
        }

        return indexes;
    }

    private sealed record ColumnDefinition(string Name, string DataType, bool IsNullable, int? MaximumLength);
}
