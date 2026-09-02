using FluentAssertions;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Administration;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Administration;
using ZMovie.Infrastructure.Analytics;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Common;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Identity;
using ZMovie.Infrastructure.Personalization;
using Xunit;
using CatalogTitle = ZMovie.Domain.Catalog.Title;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;
using EngagementUserId = ZMovie.Domain.Engagement.UserId;
using IdentityUserId = ZMovie.Domain.Identity.UserId;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class AdminTests
{
    [Theory]
    [InlineData("admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("  Admin  ", true)]
    [InlineData("member", false)]
    [InlineData("moderator", false)]
    [InlineData(null, false)]
    public void Roles_normalize_case_and_unknown_values_to_member(string? role, bool isAdmin)
    {
        Role.Normalize(role).IsAdmin.Should().Be(isAdmin);
        Role.Normalize(role).Value.Should().Be(isAdmin ? Role.AdminName : Role.MemberName);
    }

    [Theory]
    [InlineData("owner@zmovie.dev", true)]
    [InlineData("OWNER@ZMOVIE.DEV", true)]
    [InlineData(" owner@zmovie.dev ", true)]
    [InlineData("someone@zmovie.dev", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Allowlist_matches_case_insensitively(string? email, bool allowed)
    {
        var allowlist = new AdminOptions { Emails = ["owner@zmovie.dev"] };
        allowlist.IsAllowlisted(email!).Should().Be(allowed);
    }

    [Fact]
    public async Task Overview_aggregates_catalog_users_reviews_and_views()
    {
        using var database = new TestDatabase();
        var movie = MakeTitle("movie-one", "Movie One", "Phim Một", "movie", featured: true);
        var series = MakeTitle("series-one", "Series One", "Bộ Một", "series");
        database.Catalog.Titles.AddRange(movie, series);
        database.Catalog.Episodes.Add(Episode.Create(EpisodeId.New(), series.Id, 1, "Tập 1", "https://video/1"));
        database.Catalog.Genres.Add(Genre.Create(GenreId.New(), "drama", "Drama", DateTimeOffset.UtcNow));
        await database.Catalog.SaveChangesAsync();

        database.Identity.Users.AddRange(
            User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("a"), "a@test", "A", null, Role.Admin, DateTimeOffset.UtcNow),
            User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("b"), "b@test", "B", null, Role.Member, DateTimeOffset.UtcNow));
        await database.Identity.SaveChangesAsync();

        database.Engagement.TitleReviews.Add(CreateReview(movie.Id.Value, "A", 8));
        await database.Engagement.SaveChangesAsync();

        database.Analytics.TitleViewEvents.AddRange(
            TitleViewEvent.Record(ViewEventId.New(), new ZMovie.Domain.Analytics.TitleId(movie.Id.Value), null, null, "s1", DateTimeOffset.UtcNow.AddHours(-1)),
            TitleViewEvent.Record(ViewEventId.New(), new ZMovie.Domain.Analytics.TitleId(movie.Id.Value), null, null, "s2", DateTimeOffset.UtcNow.AddDays(-3)),
            TitleViewEvent.Record(ViewEventId.New(), new ZMovie.Domain.Analytics.TitleId(movie.Id.Value), null, null, "s3", DateTimeOffset.UtcNow.AddDays(-30)));
        await database.Analytics.SaveChangesAsync();

        var overview = await CreateDashboardQueries(database).GetOverviewAsync(default);

        overview.TitleCount.Should().Be(2);
        overview.MovieCount.Should().Be(1);
        overview.SeriesCount.Should().Be(1);
        overview.FeaturedCount.Should().Be(1);
        overview.EpisodeCount.Should().Be(1);
        overview.GenreCount.Should().Be(1);
        overview.UserCount.Should().Be(2);
        overview.AdminCount.Should().Be(1);
        overview.ReviewCount.Should().Be(1);
        overview.AverageRating.Should().Be(8);
        overview.ViewsLast24Hours.Should().Be(1);
        overview.ViewsLast7Days.Should().Be(2);
        overview.TopTitles.Should().ContainSingle().Which.Slug.Should().Be("movie-one");
        overview.RecentUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Title_listing_filters_and_pages_and_counts_episodes()
    {
        using var database = new TestDatabase();
        var movie = MakeTitle("movie-one", "Movie One", "Phim Một", "movie", featured: true, genre: "Action");
        var series = MakeTitle("series-one", "Series One", "Bộ Một", "series", genre: "Drama");
        database.Catalog.Titles.AddRange(movie, series);
        database.Catalog.Episodes.Add(Episode.Create(EpisodeId.New(), series.Id, 1, "Tập 1", "https://video/1"));
        database.Catalog.Episodes.Add(Episode.Create(EpisodeId.New(), series.Id, 2, "Tập 2", "https://video/2"));
        await database.Catalog.SaveChangesAsync();

        var queries = CreateDashboardQueries(database);
        var page = await queries.ListTitlesAsync(new AdminTitleFilter(null, null, null, null, 1, 1), default);
        page.Total.Should().Be(2);
        page.Items.Should().ContainSingle();

        var action = await queries.ListTitlesAsync(new AdminTitleFilter(null, "action", null, null, 1, 10), default);
        action.Items.Should().ContainSingle().Which.Slug.Should().Be("movie-one");

        var featured = await queries.ListTitlesAsync(new AdminTitleFilter(null, null, null, true, 1, 10), default);
        featured.Items.Should().ContainSingle().Which.Slug.Should().Be("movie-one");

        var seriesList = await queries.ListTitlesAsync(new AdminTitleFilter(null, null, "series", null, 1, 10), default);
        seriesList.Items.Should().ContainSingle().Which.EpisodeCount.Should().Be(2);
    }

    [Fact]
    public async Task Title_details_and_update_and_feature_toggle_modify_the_aggregate()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("original", "Original", "Gốc", "movie");
        database.Catalog.Titles.Add(title);
        await database.Catalog.SaveChangesAsync();

        var queries = CreateDashboardQueries(database);
        var catalogAdmin = CreateCatalogAdmin(database);

        var detail = await queries.GetTitleAsync("original", default);
        detail.Should().NotBeNull();
        detail!.Slug.Should().Be("original");

        var updated = await catalogAdmin.UpdateTitleAsync("original", new AdminTitleEdit(
            "Mới", "New", "Mô tả mới", "New syn", "Action", 2025, "series", "https://new/p.jpg", 120, true), default);
        updated.Should().NotBeNull();
        updated!.VietnameseTitle.Should().Be("Mới");
        updated.EnglishTitle.Should().Be("New");
        updated.Type.Should().Be("series");
        updated.Featured.Should().BeTrue();

        var toggled = await catalogAdmin.SetTitleFeaturedAsync("original", false, default);
        toggled!.Featured.Should().BeFalse();
    }

    [Fact]
    public async Task Title_deletion_removes_episodes_views_reviews_and_library_rows()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("doomed", "Doomed", "Sắp xoá", "series");
        var survivor = MakeTitle("survivor", "Survivor", "Còn lại", "movie");
        var userId = Guid.NewGuid();
        database.Catalog.Titles.AddRange(title, survivor);
        database.Catalog.Episodes.Add(Episode.Create(EpisodeId.New(), title.Id, 1, "Tập 1", "https://video/1"));
        await database.Catalog.SaveChangesAsync();

        database.Engagement.SavedTitles.Add(SavedTitle.Create(new EngagementUserId(userId), new EngagementTitleId(title.Id.Value), DateTimeOffset.UtcNow));
        database.Engagement.WatchHistory.Add(WatchProgress.Record(new EngagementUserId(userId), new ZMovie.Domain.Engagement.PlayableId(Guid.NewGuid()), new EngagementTitleId(title.Id.Value), null, WatchPosition.FromSeconds(0), DateTimeOffset.UtcNow));
        database.Engagement.TitleReviews.Add(CreateReview(title.Id.Value, "A", 5));
        database.Engagement.SavedTitles.Add(SavedTitle.Create(new EngagementUserId(userId), new EngagementTitleId(survivor.Id.Value), DateTimeOffset.UtcNow));
        await database.Engagement.SaveChangesAsync();

        database.Analytics.TitleViewEvents.Add(TitleViewEvent.Record(ViewEventId.New(), new ZMovie.Domain.Analytics.TitleId(title.Id.Value), null, null, "s", DateTimeOffset.UtcNow));
        await database.Analytics.SaveChangesAsync();

        database.Personalization.AssistantLearningEvents.Add(AssistantLearningEvent.RecordImpression(LearningEventId.New(), RecommendationId.New(), new ZMovie.Domain.Personalization.UserId(userId), new ZMovie.Domain.Personalization.TitleId(title.Id.Value), "f", 1, DateTimeOffset.UtcNow));
        await database.Personalization.SaveChangesAsync();

        var coordinator = CreateDeleteCoordinator(database);
        (await coordinator.DeleteTitleAsync("doomed", default)).Should().BeTrue();

        database.Catalog.Titles.Should().ContainSingle().Which.Slug.Value.Should().Be("survivor");
        database.Catalog.Episodes.Should().BeEmpty();
        database.Engagement.WatchHistory.Should().BeEmpty();
        database.Engagement.TitleReviews.Should().BeEmpty();
        database.Analytics.TitleViewEvents.Should().BeEmpty();
        database.Personalization.AssistantLearningEvents.Should().BeEmpty();
        database.Engagement.SavedTitles.Should().ContainSingle().Which.TitleId.Should().Be(new EngagementTitleId(survivor.Id.Value));

        (await coordinator.DeleteTitleAsync("doomed", default)).Should().BeFalse();
    }

    [Fact]
    public async Task Role_change_blocks_self_demotion_and_protects_the_last_admin()
    {
        using var database = new TestDatabase();
        var owner = User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("owner"), "owner@test", "Owner", null, Role.Admin, DateTimeOffset.UtcNow);
        var member = User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("member"), "member@test", "Member", null, Role.Member, DateTimeOffset.UtcNow);
        database.Identity.Users.AddRange(owner, member);
        await database.Identity.SaveChangesAsync();

        var users = new EfUserRepository(database.Identity);
        var queries = CreateDashboardQueries(database);
        var handler = new SetUserRoleHandler(users, queries);

        var selfDemotion = await handler.Handle(new SetUserRoleCommand(owner.Id.Value, owner.Id.Value, Role.MemberName), default);
        selfDemotion.IsError.Should().BeTrue();
        selfDemotion.FirstError.Code.Should().Be("admin.user.self_demotion");

        var missing = await handler.Handle(new SetUserRoleCommand(owner.Id.Value, Guid.NewGuid(), Role.AdminName), default);
        missing.FirstError.Code.Should().Be("admin.user.not_found");

        var promotion = await handler.Handle(new SetUserRoleCommand(owner.Id.Value, member.Id.Value, Role.AdminName), default);
        promotion.IsError.Should().BeFalse();
        promotion.Value.Role.Should().Be(Role.AdminName);

        var demotion = await handler.Handle(new SetUserRoleCommand(owner.Id.Value, member.Id.Value, Role.MemberName), default);
        demotion.IsError.Should().BeFalse();
        demotion.Value.Role.Should().Be(Role.MemberName);

        var lastAdmin = await handler.Handle(new SetUserRoleCommand(member.Id.Value, owner.Id.Value, Role.MemberName), default);
        lastAdmin.IsError.Should().BeTrue();
        lastAdmin.FirstError.Code.Should().Be("admin.user.last_admin");
    }

    [Fact]
    public async Task User_listing_filters_by_role_and_search_term()
    {
        using var database = new TestDatabase();
        database.Identity.Users.AddRange(
            User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("a"), "alice@test", "Alice", null, Role.Admin, DateTimeOffset.UtcNow),
            User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("b"), "bob@test", "Bob", null, Role.Member, DateTimeOffset.UtcNow));
        await database.Identity.SaveChangesAsync();
        var queries = CreateDashboardQueries(database);
        var users = new EfUserRepository(database.Identity);

        (await queries.ListUsersAsync(null, Role.AdminName, 1, 20, default)).Items
            .Should().ContainSingle().Which.Email.Should().Be("alice@test");
        (await queries.ListUsersAsync("bob", null, 1, 20, default)).Items
            .Should().ContainSingle().Which.DisplayName.Should().Be("Bob");
        (await queries.ListUsersAsync(null, null, 1, 20, default)).Total.Should().Be(2);
        (await queries.CountAdminsAsync(default)).Should().Be(1);
        (await queries.GetUserAsync(Guid.NewGuid(), default)).Should().BeNull();
        (await users.ChangeRoleWithLastAdminGuardAsync(new IdentityUserId(Guid.NewGuid()), Role.Admin, false, default))
            .Should().Be(ZMovie.Application.Identity.SetRoleOutcome.NotFound);
    }

    [Fact]
    public async Task Review_moderation_lists_with_title_context_and_deletes()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("phim", "Film", "Phim", "movie");
        database.Catalog.Titles.Add(title);
        await database.Catalog.SaveChangesAsync();

        var low = CreateReview(title.Id.Value, "Angry", 2, "Tệ quá");
        var high = CreateReview(title.Id.Value, "Happy", 9);
        var orphan = CreateReview(Guid.NewGuid(), "Ghost", 4);
        database.Engagement.TitleReviews.AddRange(low, high, orphan);
        await database.Engagement.SaveChangesAsync();

        var queries = CreateDashboardQueries(database);

        var all = await queries.ListReviewsAsync(null, null, 1, 20, default);
        all.Total.Should().Be(3);
        all.Items.Should().Contain(x => x.Id == orphan.Id.Value && x.TitleSlug == string.Empty);

        var lowOnly = await queries.ListReviewsAsync(null, 3, 1, 20, default);
        lowOnly.Items.Select(x => x.Id).Should().BeEquivalentTo([low.Id.Value]);
        lowOnly.Items[0].TitleName.Should().Be("Phim");

        var byComment = await queries.ListReviewsAsync("Tệ", null, 1, 20, default);
        byComment.Items.Should().ContainSingle().Which.Id.Should().Be(low.Id.Value);

        var handler = new DeleteReviewHandler(new EfReviewRepository(database.Engagement));
        (await handler.Handle(new DeleteReviewCommand(low.Id.Value), default)).Value.Should().BeTrue();
        (await handler.Handle(new DeleteReviewCommand(low.Id.Value), default)).FirstError.Code.Should().Be("admin.review.not_found");
    }

    [Fact]
    public async Task Genre_crud_counts_titles_and_rejects_duplicate_slugs()
    {
        using var database = new TestDatabase();
        database.Catalog.Titles.AddRange(
            MakeTitle("a", "A", "A", "movie", genre: "Kinh dị"),
            MakeTitle("b", "B", "B", "movie", genre: "Kinh dị"));
        await database.Catalog.SaveChangesAsync();
        var queries = CreateDashboardQueries(database);
        var catalogAdmin = CreateCatalogAdmin(database);

        var created = await catalogAdmin.CreateGenreAsync("kinh-di", "Kinh dị", default);
        created.Should().NotBeNull();
        (await catalogAdmin.CreateGenreAsync("kinh-di", "Trùng slug", default)).Should().BeNull();

        var listed = await queries.ListGenresAsync(default);
        listed.Should().ContainSingle().Which.TitleCount.Should().Be(2);

        (await catalogAdmin.UpdateGenreAsync(Guid.NewGuid(), "x", default)).Should().BeNull();
        (await catalogAdmin.DeleteGenreAsync(created!.Id, default)).Should().BeTrue();
        (await catalogAdmin.DeleteGenreAsync(created.Id, default)).Should().BeFalse();
    }

    [Fact]
    public async Task Genre_matching_handles_the_comma_joined_multi_genre_column()
    {
        using var database = new TestDatabase();
        database.Catalog.Titles.AddRange(
            MakeTitle("multi", "Multi", "Nhiều thể loại", "movie", genre: "Hành Động, Phiêu Lưu"),
            MakeTitle("single", "Single", "Một thể loại", "movie", genre: "Phiêu Lưu"),
            MakeTitle("other", "Other", "Khác", "movie", genre: "Tình Cảm"));
        database.Catalog.Genres.AddRange(
            Genre.Create(GenreId.New(), "hanh-dong", "Hành Động", DateTimeOffset.UtcNow),
            Genre.Create(GenreId.New(), "phieu-luu", "Phiêu Lưu", DateTimeOffset.UtcNow),
            Genre.Create(GenreId.New(), "tinh-cam", "Tình Cảm", DateTimeOffset.UtcNow));
        await database.Catalog.SaveChangesAsync();
        var queries = CreateDashboardQueries(database);

        var action = await queries.ListTitlesAsync(new AdminTitleFilter(null, "Hành Động", null, null, 1, 20), default);
        action.Items.Select(x => x.Slug).Should().BeEquivalentTo(["multi"]);

        var adventure = await queries.ListTitlesAsync(new AdminTitleFilter(null, "Phiêu Lưu", null, null, 1, 20), default);
        adventure.Items.Select(x => x.Slug).Should().BeEquivalentTo(["multi", "single"]);

        var counts = (await queries.ListGenresAsync(default)).ToDictionary(x => x.Name, x => x.TitleCount);
        counts["Hành Động"].Should().Be(1);
        counts["Phiêu Lưu"].Should().Be(2);
        counts["Tình Cảm"].Should().Be(1);
    }

    [Fact]
    public async Task Renaming_a_genre_rewrites_the_name_on_every_title_that_carries_it()
    {
        using var database = new TestDatabase();
        database.Catalog.Titles.AddRange(
            MakeTitle("multi", "Multi", "Nhiều", "movie", genre: "Hành Động, Phiêu Lưu"),
            MakeTitle("solo", "Solo", "Một", "movie", genre: "Hành Động"),
            MakeTitle("untouched", "Untouched", "Không đổi", "movie", genre: "Tình Cảm"));
        var genre = Genre.Create(GenreId.New(), "hanh-dong", "Hành Động", DateTimeOffset.UtcNow);
        database.Catalog.Genres.Add(genre);
        await database.Catalog.SaveChangesAsync();
        var catalogAdmin = CreateCatalogAdmin(database);

        var renamed = await catalogAdmin.UpdateGenreAsync(genre.Id.Value, "Hành Động Mới", default);

        renamed!.Name.Should().Be("Hành Động Mới");
        renamed.TitleCount.Should().Be(2);
        var multiSlug = TitleSlug.Parse("multi");
        var soloSlug = TitleSlug.Parse("solo");
        var untouchedSlug = TitleSlug.Parse("untouched");
        database.Catalog.Titles.Single(x => x.Slug == multiSlug).Genre.Should().Be("Hành Động Mới, Phiêu Lưu");
        database.Catalog.Titles.Single(x => x.Slug == soloSlug).Genre.Should().Be("Hành Động Mới");
        database.Catalog.Titles.Single(x => x.Slug == untouchedSlug).Genre.Should().Be("Tình Cảm");
    }

    [Fact]
    public async Task Admin_search_is_case_insensitive_across_titles_users_and_reviews()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("phim-hay", "Great Film", "Phim Hay", "movie");
        database.Catalog.Titles.Add(title);
        await database.Catalog.SaveChangesAsync();

        database.Identity.Users.Add(User.Create(new IdentityUserId(Guid.NewGuid()), new ExternalIdentity("s"), "Alice@Test", "Nguyễn Văn A", null, Role.Member, DateTimeOffset.UtcNow));
        await database.Identity.SaveChangesAsync();

        database.Engagement.TitleReviews.Add(CreateReview(title.Id.Value, "Bob", 5, "Rất Hay"));
        await database.Engagement.SaveChangesAsync();

        var queries = CreateDashboardQueries(database);

        (await queries.ListTitlesAsync(new AdminTitleFilter("phim hay", null, null, null, 1, 20), default))
            .Items.Should().ContainSingle();
        (await queries.ListTitlesAsync(new AdminTitleFilter("GREAT", null, null, null, 1, 20), default))
            .Items.Should().ContainSingle();
        (await queries.ListUsersAsync("alice", null, 1, 20, default)).Items.Should().ContainSingle();
        (await queries.ListUsersAsync("nguyễn", null, 1, 20, default)).Items.Should().ContainSingle();
        (await queries.ListReviewsAsync("bob", null, 1, 20, default)).Items.Should().ContainSingle();
        (await queries.ListReviewsAsync("rất hay", null, 1, 20, default)).Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Admin_query_handlers_normalize_paging_and_surface_not_found()
    {
        using var database = new TestDatabase();
        database.Catalog.Titles.Add(MakeTitle("only", "Only", "Duy nhất", "movie"));
        await database.Catalog.SaveChangesAsync();
        var queries = CreateDashboardQueries(database);
        var coordinator = CreateDeleteCoordinator(database);

        var listed = await new ListAdminTitlesHandler(queries)
            .Handle(new ListAdminTitlesQuery(null, null, null, null, null, null), default);
        listed.Value.Page.Should().Be(1);
        listed.Value.PageSize.Should().Be(AdminPaging.DefaultPageSize);

        var oversized = await new ListAdminTitlesHandler(queries)
            .Handle(new ListAdminTitlesQuery(null, null, null, null, 0, 5_000), default);
        oversized.Value.Page.Should().Be(1);
        oversized.Value.PageSize.Should().Be(AdminPaging.MaxPageSize);

        var found = await new GetAdminTitleHandler(queries).Handle(new GetAdminTitleQuery(" only "), default);
        found.IsError.Should().BeFalse();
        found.Value.Slug.Should().Be("only");

        var missing = await new GetAdminTitleHandler(queries).Handle(new GetAdminTitleQuery("nope"), default);
        missing.FirstError.Code.Should().Be("admin.title.not_found");

        var deleted = await new DeleteAdminTitleHandler(coordinator).Handle(new DeleteAdminTitleCommand("nope"), default);
        deleted.FirstError.Type.Should().Be(ErrorOr.ErrorType.NotFound);
    }

    [Fact]
    public void Title_update_validator_rejects_bad_type_year_and_poster()
    {
        var validator = new UpdateAdminTitleValidator();
        var valid = new AdminTitleEdit("Tên", "Name", "Mô tả", "Synopsis", "Drama", 2020, "movie", "https://cdn/p.jpg", 100, false);

        validator.Validate(new UpdateAdminTitleCommand("slug", valid)).IsValid.Should().BeTrue();
        validator.Validate(new UpdateAdminTitleCommand("slug", valid with { Type = "documentary" })).IsValid.Should().BeFalse();
        validator.Validate(new UpdateAdminTitleCommand("slug", valid with { Year = 1000 })).IsValid.Should().BeFalse();
        validator.Validate(new UpdateAdminTitleCommand("slug", valid with { PosterUrl = "poster" })).IsValid.Should().BeFalse();
        validator.Validate(new UpdateAdminTitleCommand("slug", valid with { PosterUrl = "javascript:alert(1)" })).IsValid.Should().BeFalse();
        validator.Validate(new UpdateAdminTitleCommand("slug", valid with { RuntimeMinutes = -1 })).IsValid.Should().BeFalse();
        validator.Validate(new UpdateAdminTitleCommand(string.Empty, valid)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Genre_and_role_validators_enforce_slug_shape_and_known_roles()
    {
        var genre = new CreateAdminGenreValidator();
        genre.Validate(new CreateAdminGenreCommand("kinh-di", "Kinh dị")).IsValid.Should().BeTrue();
        genre.Validate(new CreateAdminGenreCommand("Kinh Di", "Kinh dị")).IsValid.Should().BeFalse();
        genre.Validate(new CreateAdminGenreCommand("kinh--di", "Kinh dị")).IsValid.Should().BeFalse();
        genre.Validate(new CreateAdminGenreCommand("-kinh", "Kinh dị")).IsValid.Should().BeFalse();
        genre.Validate(new CreateAdminGenreCommand("kinh-di", "")).IsValid.Should().BeFalse();

        var role = new SetUserRoleValidator();
        role.Validate(new SetUserRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "admin")).IsValid.Should().BeTrue();
        role.Validate(new SetUserRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "superuser")).IsValid.Should().BeFalse();
        role.Validate(new SetUserRoleCommand(Guid.NewGuid(), Guid.Empty, "admin")).IsValid.Should().BeFalse();
    }

    private static EfAdminDashboardQueries CreateDashboardQueries(TestDatabase database) =>
        new(database.Catalog, database.Identity, database.Engagement, database.Analytics);

    private static AdminTitleDeletionCoordinator CreateDeleteCoordinator(TestDatabase database) =>
        new(
            new EfCatalogTitleCleanupPort(database.Catalog),
            new EfEngagementTitleCleanupPort(database.Engagement),
            new EfAnalyticsTitleCleanupPort(database.Analytics),
            new EfPersonalizationTitleCleanupPort(database.Personalization),
            new NpgsqlTransactionCoordinator(database.Catalog, database.Identity, database.Engagement, database.Analytics, database.Personalization));

    private static EfCatalogAdministrationService CreateCatalogAdmin(TestDatabase database) =>
        new(database.Catalog, CreateDashboardQueries(database));

    private static CatalogTitle MakeTitle(string slug, string english, string vietnamese, string type, bool featured = false, string genre = "Drama") =>
        CatalogTitle.Create(
            CatalogTitleId.New(),
            TitleSlug.Parse(slug),
            new LocalizedText(vietnamese, english),
            new LocalizedText("Mô tả", "Synopsis"),
            genre,
            ReleaseYear.FromInt(2026),
            TitleType.Normalize(type),
            "https://cdn/poster.jpg",
            Runtime.FromMinutes(90),
            featured,
            DateTimeOffset.UtcNow);

    private static Review CreateReview(Guid titleId, string authorName, int rating, string? comment = null) =>
        Review.Create(
            ReviewId.New(),
            new EngagementTitleId(titleId),
            new EngagementUserId(Guid.NewGuid()),
            authorName,
            rating,
            comment,
            DateTimeOffset.UtcNow).Review!;
}
