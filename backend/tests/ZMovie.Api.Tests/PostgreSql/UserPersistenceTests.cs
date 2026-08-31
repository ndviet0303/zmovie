using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Identity;
using ZMovie.Infrastructure.Identity.Persistence;
using Xunit;

namespace ZMovie.Api.Tests.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class UserPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 31, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Ef_mapping_matches_the_existing_users_schema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.MigrateAsync();

        var userEntity = context.Model.FindEntityType(typeof(User));
        userEntity.Should().NotBeNull();
        userEntity!.GetTableName().Should().Be("users");
        userEntity.GetSchema().Should().Be("public");

        var userTable = StoreObjectIdentifier.Table("users", "public");
        userEntity.FindPrimaryKey()!.Properties.Select(p => p.Name)
            .Should().Equal(nameof(User.Id));
        userEntity.FindPrimaryKey()!.GetName(userTable).Should().Be("pk_users");

        var indexes = userEntity.GetIndexes().ToDictionary(i => i.GetDatabaseName()!);
        indexes["ix_users_google_subject"].IsUnique.Should().BeTrue();
        indexes["ix_users_google_subject"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(User.ExternalIdentity));
        indexes["ix_users_email"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(User.Email));
        indexes["ix_users_role_created_at"].Properties.Select(p => p.Name)
            .Should().Equal(nameof(User.Role), nameof(User.CreatedAt));

        AssertProperty(userEntity, userTable, nameof(User.Id), "id", typeof(UserId), typeof(Guid), false, null);
        AssertProperty(userEntity, userTable, nameof(User.ExternalIdentity), "google_subject", typeof(ExternalIdentity), typeof(string), false, 128);
        AssertProperty(userEntity, userTable, nameof(User.Email), "email", typeof(string), typeof(string), false, 320);
        AssertProperty(userEntity, userTable, nameof(User.DisplayName), "display_name", typeof(string), typeof(string), false, 300);
        AssertProperty(userEntity, userTable, nameof(User.AvatarUrl), "avatar_url", typeof(string), typeof(string), true, 2000);
        AssertProperty(userEntity, userTable, nameof(User.Role), "role", typeof(Role), typeof(string), false, 32);
        AssertProperty(userEntity, userTable, nameof(User.CreatedAt), "created_at", typeof(DateTimeOffset), typeof(DateTimeOffset), false, null);
        AssertProperty(userEntity, userTable, nameof(User.LastSignedInAt), "last_signed_in_at", typeof(DateTimeOffset), typeof(DateTimeOffset), false, null);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Repository_and_queries_roundtrip_user_on_postgresql()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var userId = new UserId(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var externalId = new ExternalIdentity("google-sub-roundtrip");
        var user = User.Create(userId, externalId, "roundtrip@test.com", "Roundtrip User", "https://avatar/1.png", Role.Admin, OccurredAt);

        await using (var writeContext = CreateContext(database.ConnectionString))
        {
            await writeContext.Database.MigrateAsync();
            var repo = new EfUserRepository(writeContext);
            repo.Add(user);
            await repo.SaveChangesAsync(default);
        }

        await using var readContext = CreateContext(database.ConnectionString);
        var repoRead = new EfUserRepository(readContext);
        var materialized = await repoRead.FindByIdAsync(userId, default);

        materialized.Should().NotBeNull();
        materialized!.Id.Should().Be(userId);
        materialized.ExternalIdentity.Should().Be(externalId);
        materialized.Email.Should().Be("roundtrip@test.com");
        materialized.DisplayName.Should().Be("Roundtrip User");
        materialized.AvatarUrl.Should().Be("https://avatar/1.png");
        materialized.Role.Should().Be(Role.Admin);
        materialized.CreatedAt.Should().Be(OccurredAt);
        materialized.LastSignedInAt.Should().Be(OccurredAt);

        var queries = new EfUserQueries(readContext);
        var count = await queries.CountAdminsAsync(default);
        count.Should().Be(1);

        var summary = await queries.GetUserAsync(userId, default);
        summary.Should().NotBeNull();
        summary!.Role.Should().Be(Role.AdminName);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public async Task Two_simultaneous_demotions_cannot_remove_the_last_admin()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var admin1Id = new UserId(Guid.Parse("11111111-1111-4111-8111-111111111111"));
        var admin2Id = new UserId(Guid.Parse("22222222-2222-4222-8222-222222222222"));

        await using (var setupContext = CreateContext(database.ConnectionString))
        {
            await setupContext.Database.MigrateAsync();
            var repo = new EfUserRepository(setupContext);
            repo.Add(User.Create(admin1Id, new ExternalIdentity("sub-1"), "admin1@test.com", "Admin 1", null, Role.Admin, OccurredAt));
            repo.Add(User.Create(admin2Id, new ExternalIdentity("sub-2"), "admin2@test.com", "Admin 2", null, Role.Admin, OccurredAt));
            await repo.SaveChangesAsync(default);
        }

        // Run two concurrent demotions
        var task1 = Task.Run(async () =>
        {
            try
            {
                await using var context = CreateContext(database.ConnectionString);
                var repo = new EfUserRepository(context);
                return await repo.ChangeRoleWithLastAdminGuardAsync(admin1Id, Role.Member, true, default);
            }
            catch (PostgresException ex) when (ex.SqlState == "40001") // serialization failure
            {
                return SetRoleOutcome.LastAdmin;
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                await using var context = CreateContext(database.ConnectionString);
                var repo = new EfUserRepository(context);
                return await repo.ChangeRoleWithLastAdminGuardAsync(admin2Id, Role.Member, true, default);
            }
            catch (PostgresException ex) when (ex.SqlState == "40001") // serialization failure
            {
                return SetRoleOutcome.LastAdmin;
            }
        });

        var results = await Task.WhenAll(task1, task2);

        // Exactly one demotion must succeed, the other must be rejected or hit serializable conflict
        results.Count(r => r == SetRoleOutcome.Updated).Should().Be(1);

        // Verify in database: exactly 1 admin remains
        await using var verifyContext = CreateContext(database.ConnectionString);
        var adminCount = await new EfUserQueries(verifyContext).CountAdminsAsync(default);
        adminCount.Should().Be(1);
    }

    private static IdentityDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new IdentityDbContext(options);
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
