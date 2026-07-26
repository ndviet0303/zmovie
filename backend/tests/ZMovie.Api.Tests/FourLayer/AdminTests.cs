using FluentAssertions;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Administration;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Administration;
using ZMovie.Infrastructure.Identity;
using Xunit;

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
        ZMovieRoles.IsAdmin(role).Should().Be(isAdmin);
        ZMovieRoles.Normalize(role).Should().Be(isAdmin ? ZMovieRoles.Admin : ZMovieRoles.Member);
    }

    [Theory]
    [InlineData("owner@zmovie.dev", true)]
    [InlineData("OWNER@ZMOVIE.DEV", true)]
    [InlineData(" owner@zmovie.dev ", true)]
    [InlineData("someone@zmovie.dev", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Admin_allowlist_matches_case_insensitively_and_ignores_blanks(string? email, bool expected)
    {
        var options = new AdminOptions { Emails = ["Owner@zmovie.dev", "  ", ""] };
        options.IsAllowlisted(email).Should().Be(expected);
    }

    [Fact]
    public async Task Overview_reports_catalog_engagement_and_identity_counts()
    {
        using var database = new TestDatabase();
        var movie = Title("movie-one", "Movie One", "Phim Một", "movie", featured: true);
        var series = Title("series-one", "Series One", "Phim Bộ Một", "series");
        database.Db.Titles.AddRange(movie, series);
        database.Db.Episodes.Add(new CatalogEpisode { TitleId = series.Id, Number = 1, Name = "Tập 1", HlsUrl = "https://video/1" });
        database.Db.Genres.Add(new CatalogGenre { Slug = "drama", Name = "Drama" });
        database.Db.Users.AddRange(
            new ZMovieUser { GoogleSubject = "a", Email = "a@test", DisplayName = "A", Role = ZMovieRoles.Admin },
            new ZMovieUser { GoogleSubject = "b", Email = "b@test", DisplayName = "B" });
        database.Db.TitleReviews.Add(new TitleReview { TitleId = movie.Id, UserId = Guid.NewGuid(), AuthorName = "A", Rating = 8 });
        database.Db.TitleViewEvents.AddRange(
            new TitleViewEvent { TitleId = movie.Id, SessionId = "s1", ViewedAt = DateTimeOffset.UtcNow.AddHours(-1) },
            new TitleViewEvent { TitleId = movie.Id, SessionId = "s2", ViewedAt = DateTimeOffset.UtcNow.AddDays(-3) },
            new TitleViewEvent { TitleId = movie.Id, SessionId = "s3", ViewedAt = DateTimeOffset.UtcNow.AddDays(-30) });
        await database.Db.SaveChangesAsync();

        var overview = await new EfAdminStore(database.Db).GetOverviewAsync(default);

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
        var series = Title("bo-phim", "Series", "Bộ phim", "series", genre: "Action");
        database.Db.Titles.AddRange(
            series,
            Title("phim-le", "Solo", "Phim lẻ", "movie", genre: "Drama", featured: true),
            Title("khac", "Other", "Khác", "movie", genre: "Action"));
        database.Db.Episodes.AddRange(
            new CatalogEpisode { TitleId = series.Id, Number = 1, Name = "Tập 1", HlsUrl = "https://video/1" },
            new CatalogEpisode { TitleId = series.Id, Number = 2, Name = "Tập 2", HlsUrl = "https://video/2" });
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var byType = await store.ListTitlesAsync(new AdminTitleFilter(null, null, "series", null, 1, 20), default);
        byType.Total.Should().Be(1);
        byType.Items.Should().ContainSingle().Which.EpisodeCount.Should().Be(2);

        var byGenre = await store.ListTitlesAsync(new AdminTitleFilter(null, "Action", null, null, 1, 20), default);
        byGenre.Total.Should().Be(2);

        var byFeatured = await store.ListTitlesAsync(new AdminTitleFilter(null, null, null, true, 1, 20), default);
        byFeatured.Items.Should().ContainSingle().Which.Slug.Should().Be("phim-le");

        var bySearch = await store.ListTitlesAsync(new AdminTitleFilter("phim-le", null, null, null, 1, 20), default);
        bySearch.Items.Should().ContainSingle().Which.Slug.Should().Be("phim-le");

        var firstPage = await store.ListTitlesAsync(new AdminTitleFilter(null, null, null, null, 1, 2), default);
        firstPage.Items.Should().HaveCount(2);
        firstPage.Total.Should().Be(3);
        firstPage.PageCount.Should().Be(2);

        var secondPage = await store.ListTitlesAsync(new AdminTitleFilter(null, null, null, null, 2, 2), default);
        secondPage.Items.Should().ContainSingle();
        secondPage.Items.Select(x => x.Slug).Should().NotIntersectWith(firstPage.Items.Select(x => x.Slug));
    }

    [Fact]
    public async Task Updating_a_title_writes_every_editable_field_and_bumps_updated_at()
    {
        using var database = new TestDatabase();
        var title = Title("slug", "English", "Tiếng Việt", "movie");
        var originalUpdatedAt = title.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2);
        database.Db.Titles.Add(title);
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var edit = new AdminTitleEdit("Tên mới", "New name", "Mô tả mới", "New synopsis", "Horror", 1999, "series", "https://cdn/poster.jpg", 128, true);
        var updated = await store.UpdateTitleAsync("slug", edit, default);

        updated.Should().NotBeNull();
        updated!.VietnameseTitle.Should().Be("Tên mới");
        updated.EnglishTitle.Should().Be("New name");
        updated.VietnameseSynopsis.Should().Be("Mô tả mới");
        updated.EnglishSynopsis.Should().Be("New synopsis");
        updated.Genre.Should().Be("Horror");
        updated.Year.Should().Be(1999);
        updated.Type.Should().Be("series");
        updated.PosterUrl.Should().Be("https://cdn/poster.jpg");
        updated.RuntimeMinutes.Should().Be(128);
        updated.Featured.Should().BeTrue();
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);

        (await store.UpdateTitleAsync("missing", edit, default)).Should().BeNull();
        (await store.SetTitleFeaturedAsync("slug", false, default))!.Featured.Should().BeFalse();
        (await store.SetTitleFeaturedAsync("missing", true, default)).Should().BeNull();
    }

    [Fact]
    public async Task Deleting_a_title_removes_every_dependent_row()
    {
        using var database = new TestDatabase();
        var title = Title("doomed", "Doomed", "Sắp xoá", "series");
        var survivor = Title("survivor", "Survivor", "Còn lại", "movie");
        var userId = Guid.NewGuid();
        database.Db.Titles.AddRange(title, survivor);
        database.Db.Episodes.Add(new CatalogEpisode { TitleId = title.Id, Number = 1, Name = "Tập 1", HlsUrl = "https://video/1" });
        database.Db.SavedTitles.Add(new SavedTitle { UserId = userId, TitleId = title.Id });
        database.Db.WatchHistory.Add(new WatchProgress { UserId = userId, PlayableId = Guid.NewGuid(), TitleId = title.Id });
        database.Db.TitleReviews.Add(new TitleReview { TitleId = title.Id, UserId = userId, AuthorName = "A", Rating = 5 });
        database.Db.TitleViewEvents.Add(new TitleViewEvent { TitleId = title.Id, SessionId = "s" });
        database.Db.AssistantLearningEvents.Add(new AssistantLearningEvent { RecommendationId = Guid.NewGuid(), UserId = userId, TitleId = title.Id, Features = "f", EventType = "impression" });
        // A row belonging to another title must be left untouched.
        database.Db.SavedTitles.Add(new SavedTitle { UserId = userId, TitleId = survivor.Id });
        await database.Db.SaveChangesAsync();

        var store = new EfAdminStore(database.Db);
        (await store.DeleteTitleAsync("doomed", default)).Should().BeTrue();

        database.Db.Titles.Should().ContainSingle().Which.Slug.Should().Be("survivor");
        database.Db.Episodes.Should().BeEmpty();
        database.Db.WatchHistory.Should().BeEmpty();
        database.Db.TitleReviews.Should().BeEmpty();
        database.Db.TitleViewEvents.Should().BeEmpty();
        database.Db.AssistantLearningEvents.Should().BeEmpty();
        database.Db.SavedTitles.Should().ContainSingle().Which.TitleId.Should().Be(survivor.Id);

        (await store.DeleteTitleAsync("doomed", default)).Should().BeFalse();
    }

    [Fact]
    public async Task Role_change_blocks_self_demotion_and_protects_the_last_admin()
    {
        using var database = new TestDatabase();
        var owner = new ZMovieUser { GoogleSubject = "owner", Email = "owner@test", DisplayName = "Owner", Role = ZMovieRoles.Admin };
        var member = new ZMovieUser { GoogleSubject = "member", Email = "member@test", DisplayName = "Member" };
        database.Db.Users.AddRange(owner, member);
        await database.Db.SaveChangesAsync();

        var store = new EfAdminStore(database.Db);
        var handler = new SetUserRoleHandler(store);

        var selfDemotion = await handler.Handle(new SetUserRoleCommand(owner.Id, owner.Id, ZMovieRoles.Member), default);
        selfDemotion.IsError.Should().BeTrue();
        selfDemotion.FirstError.Code.Should().Be("admin.user.self_demotion");

        var missing = await handler.Handle(new SetUserRoleCommand(owner.Id, Guid.NewGuid(), ZMovieRoles.Admin), default);
        missing.FirstError.Code.Should().Be("admin.user.not_found");

        var promotion = await handler.Handle(new SetUserRoleCommand(owner.Id, member.Id, ZMovieRoles.Admin), default);
        promotion.IsError.Should().BeFalse();
        promotion.Value.Role.Should().Be(ZMovieRoles.Admin);

        // Now that there are two admins, demoting one is allowed.
        var demotion = await handler.Handle(new SetUserRoleCommand(owner.Id, member.Id, ZMovieRoles.Member), default);
        demotion.IsError.Should().BeFalse();
        demotion.Value.Role.Should().Be(ZMovieRoles.Member);

        // Owner is the only admin left, so a second admin cannot demote them either.
        var lastAdmin = await handler.Handle(new SetUserRoleCommand(member.Id, owner.Id, ZMovieRoles.Member), default);
        lastAdmin.IsError.Should().BeTrue();
        lastAdmin.FirstError.Code.Should().Be("admin.user.last_admin");
    }

    [Fact]
    public async Task User_listing_filters_by_role_and_search_term()
    {
        using var database = new TestDatabase();
        database.Db.Users.AddRange(
            new ZMovieUser { GoogleSubject = "a", Email = "alice@test", DisplayName = "Alice", Role = ZMovieRoles.Admin },
            new ZMovieUser { GoogleSubject = "b", Email = "bob@test", DisplayName = "Bob" });
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        (await store.ListUsersAsync(null, ZMovieRoles.Admin, 1, 20, default)).Items
            .Should().ContainSingle().Which.Email.Should().Be("alice@test");
        (await store.ListUsersAsync("bob", null, 1, 20, default)).Items
            .Should().ContainSingle().Which.DisplayName.Should().Be("Bob");
        (await store.ListUsersAsync(null, null, 1, 20, default)).Total.Should().Be(2);
        (await store.CountAdminsAsync(default)).Should().Be(1);
        (await store.GetUserAsync(Guid.NewGuid(), default)).Should().BeNull();
        (await store.SetUserRoleAsync(Guid.NewGuid(), ZMovieRoles.Admin, false, default)).Outcome
            .Should().Be(SetRoleOutcome.NotFound);
    }

    [Fact]
    public async Task Review_moderation_lists_with_title_context_and_deletes()
    {
        using var database = new TestDatabase();
        var title = Title("phim", "Film", "Phim", "movie");
        database.Db.Titles.Add(title);
        var low = new TitleReview { TitleId = title.Id, UserId = Guid.NewGuid(), AuthorName = "Angry", Rating = 2, Comment = "Tệ quá" };
        var high = new TitleReview { TitleId = title.Id, UserId = Guid.NewGuid(), AuthorName = "Happy", Rating = 9 };
        var orphan = new TitleReview { TitleId = Guid.NewGuid(), UserId = Guid.NewGuid(), AuthorName = "Ghost", Rating = 4 };
        database.Db.TitleReviews.AddRange(low, high, orphan);
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var all = await store.ListReviewsAsync(null, null, 1, 20, default);
        all.Total.Should().Be(3);
        all.Items.Should().Contain(x => x.Id == orphan.Id && x.TitleSlug == string.Empty);

        var lowOnly = await store.ListReviewsAsync(null, 3, 1, 20, default);
        lowOnly.Items.Select(x => x.Id).Should().BeEquivalentTo([low.Id]);
        lowOnly.Items[0].TitleName.Should().Be("Phim");

        var byComment = await store.ListReviewsAsync("Tệ", null, 1, 20, default);
        byComment.Items.Should().ContainSingle().Which.Id.Should().Be(low.Id);

        (await store.DeleteReviewAsync(low.Id, default)).Should().BeTrue();
        (await store.DeleteReviewAsync(low.Id, default)).Should().BeFalse();
    }

    [Fact]
    public async Task Genre_crud_counts_titles_and_rejects_duplicate_slugs()
    {
        using var database = new TestDatabase();
        database.Db.Titles.AddRange(
            Title("a", "A", "A", "movie", genre: "Kinh dị"),
            Title("b", "B", "B", "movie", genre: "Kinh dị"));
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var created = await store.CreateGenreAsync("kinh-di", "Kinh dị", default);
        created.Should().NotBeNull();
        (await store.CreateGenreAsync("kinh-di", "Trùng slug", default)).Should().BeNull();

        var listed = await store.ListGenresAsync(default);
        listed.Should().ContainSingle().Which.TitleCount.Should().Be(2);

        (await store.UpdateGenreAsync(Guid.NewGuid(), "x", default)).Should().BeNull();
        (await store.DeleteGenreAsync(created!.Id, default)).Should().BeTrue();
        (await store.DeleteGenreAsync(created.Id, default)).Should().BeFalse();
    }

    [Fact]
    public async Task Genre_matching_handles_the_comma_joined_multi_genre_column()
    {
        // titles.genre is written by the OPhim importer as "Hành Động, Phiêu Lưu", so exact
        // equality against a single genre name would match nothing.
        using var database = new TestDatabase();
        database.Db.Titles.AddRange(
            Title("multi", "Multi", "Nhiều thể loại", "movie", genre: "Hành Động, Phiêu Lưu"),
            Title("single", "Single", "Một thể loại", "movie", genre: "Phiêu Lưu"),
            Title("other", "Other", "Khác", "movie", genre: "Tình Cảm"));
        database.Db.Genres.AddRange(
            new CatalogGenre { Slug = "hanh-dong", Name = "Hành Động" },
            new CatalogGenre { Slug = "phieu-luu", Name = "Phiêu Lưu" },
            new CatalogGenre { Slug = "tinh-cam", Name = "Tình Cảm" });
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var action = await store.ListTitlesAsync(new AdminTitleFilter(null, "Hành Động", null, null, 1, 20), default);
        action.Items.Select(x => x.Slug).Should().BeEquivalentTo(["multi"]);

        var adventure = await store.ListTitlesAsync(new AdminTitleFilter(null, "Phiêu Lưu", null, null, 1, 20), default);
        adventure.Items.Select(x => x.Slug).Should().BeEquivalentTo(["multi", "single"]);

        var counts = (await store.ListGenresAsync(default)).ToDictionary(x => x.Name, x => x.TitleCount);
        counts["Hành Động"].Should().Be(1);
        counts["Phiêu Lưu"].Should().Be(2);
        counts["Tình Cảm"].Should().Be(1);
    }

    [Fact]
    public async Task Renaming_a_genre_rewrites_the_name_on_every_title_that_carries_it()
    {
        using var database = new TestDatabase();
        database.Db.Titles.AddRange(
            Title("multi", "Multi", "Nhiều", "movie", genre: "Hành Động, Phiêu Lưu"),
            Title("solo", "Solo", "Một", "movie", genre: "Hành Động"),
            Title("untouched", "Untouched", "Không đổi", "movie", genre: "Tình Cảm"));
        var genre = new CatalogGenre { Slug = "hanh-dong", Name = "Hành Động" };
        database.Db.Genres.Add(genre);
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var renamed = await store.UpdateGenreAsync(genre.Id, "Hành Động Mới", default);

        renamed!.Name.Should().Be("Hành Động Mới");
        renamed.TitleCount.Should().Be(2);
        database.Db.Titles.Single(x => x.Slug == "multi").Genre.Should().Be("Hành Động Mới, Phiêu Lưu");
        database.Db.Titles.Single(x => x.Slug == "solo").Genre.Should().Be("Hành Động Mới");
        database.Db.Titles.Single(x => x.Slug == "untouched").Genre.Should().Be("Tình Cảm");
    }

    [Fact]
    public async Task Admin_search_is_case_insensitive_across_titles_users_and_reviews()
    {
        using var database = new TestDatabase();
        var title = Title("phim-hay", "Great Film", "Phim Hay", "movie");
        database.Db.Titles.Add(title);
        database.Db.Users.Add(new ZMovieUser { GoogleSubject = "s", Email = "Alice@Test", DisplayName = "Nguyễn Văn A" });
        database.Db.TitleReviews.Add(new TitleReview { TitleId = title.Id, UserId = Guid.NewGuid(), AuthorName = "Bob", Rating = 5, Comment = "Rất Hay" });
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        (await store.ListTitlesAsync(new AdminTitleFilter("phim hay", null, null, null, 1, 20), default))
            .Items.Should().ContainSingle();
        (await store.ListTitlesAsync(new AdminTitleFilter("GREAT", null, null, null, 1, 20), default))
            .Items.Should().ContainSingle();
        (await store.ListUsersAsync("alice", null, 1, 20, default)).Items.Should().ContainSingle();
        (await store.ListUsersAsync("nguyễn", null, 1, 20, default)).Items.Should().ContainSingle();
        (await store.ListReviewsAsync("bob", null, 1, 20, default)).Items.Should().ContainSingle();
        (await store.ListReviewsAsync("rất hay", null, 1, 20, default)).Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Admin_query_handlers_normalize_paging_and_surface_not_found()
    {
        using var database = new TestDatabase();
        database.Db.Titles.Add(Title("only", "Only", "Duy nhất", "movie"));
        await database.Db.SaveChangesAsync();
        var store = new EfAdminStore(database.Db);

        var listed = await new ListAdminTitlesHandler(store)
            .Handle(new ListAdminTitlesQuery(null, null, null, null, null, null), default);
        listed.Value.Page.Should().Be(1);
        listed.Value.PageSize.Should().Be(AdminPaging.DefaultPageSize);

        var oversized = await new ListAdminTitlesHandler(store)
            .Handle(new ListAdminTitlesQuery(null, null, null, null, 0, 5_000), default);
        oversized.Value.Page.Should().Be(1);
        oversized.Value.PageSize.Should().Be(AdminPaging.MaxPageSize);

        var found = await new GetAdminTitleHandler(store).Handle(new GetAdminTitleQuery(" only "), default);
        found.IsError.Should().BeFalse();
        found.Value.Slug.Should().Be("only");

        var missing = await new GetAdminTitleHandler(store).Handle(new GetAdminTitleQuery("nope"), default);
        missing.FirstError.Code.Should().Be("admin.title.not_found");

        var deleted = await new DeleteAdminTitleHandler(store).Handle(new DeleteAdminTitleCommand("nope"), default);
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

    private static CatalogTitle Title(string slug, string english, string vietnamese, string type, bool featured = false, string genre = "Drama") => new()
    {
        Slug = slug,
        EnglishTitle = english,
        VietnameseTitle = vietnamese,
        EnglishSynopsis = "Synopsis",
        VietnameseSynopsis = "Mô tả",
        Genre = genre,
        Year = 2026,
        Type = type,
        PosterUrl = "https://cdn/poster.jpg",
        RuntimeMinutes = 90,
        Featured = featured,
    };
}
