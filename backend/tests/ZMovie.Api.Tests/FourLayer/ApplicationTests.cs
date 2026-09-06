using FluentAssertions;
using FluentValidation;
using MediatR;
using ErrorOr;
using ZMovie.Application.Analytics;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Application.Identity;
using ZMovie.Application.Search;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Engagement;
using Xunit;
using EngagementUserId = ZMovie.Domain.Engagement.UserId;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;
using EngagementPlayableId = ZMovie.Domain.Engagement.PlayableId;
using AnalyticsUserId = ZMovie.Domain.Analytics.UserId;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class ApplicationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FirstTitleId = Guid.NewGuid();
    private static readonly Guid SecondTitleId = Guid.NewGuid();
    private static readonly LibraryTitle FirstTitle = new("first", "First", "Drama", 2026, "movie", "poster", 100);
    private static readonly LibraryTitle SecondTitle = new("second", "Second", "Action", 2025, "series", "poster-2", 24);

    [Fact]
    public async Task Library_and_discovery_handlers_map_entries_and_filter_missing_titles()
    {
        var store = new FakeLibraryStore
        {
            Saved = [new(FirstTitleId, DateTimeOffset.UtcNow), new(Guid.NewGuid(), DateTimeOffset.UtcNow)],
            History = [new(SecondTitleId, Guid.NewGuid(), 2, 42, DateTimeOffset.UtcNow), new(Guid.NewGuid(), Guid.NewGuid(), null, 1, DateTimeOffset.UtcNow)]
        };
        var catalog = new FakeLibraryCatalog
        {
            Titles = new Dictionary<Guid, LibraryTitle> { [FirstTitleId] = FirstTitle, [SecondTitleId] = SecondTitle },
            Candidates = [new(FirstTitleId, FirstTitle, "drama"), new(SecondTitleId, SecondTitle, "action")]
        };
        var library = await new GetUserLibraryHandler(store, catalog).Handle(new(UserId, "en"), CancellationToken.None);
        library.IsError.Should().BeFalse();
        library.Value.Saved.Should().ContainSingle().Which.Should().Be(FirstTitle);
        library.Value.History.Should().ContainSingle().Which.EpisodeNumber.Should().Be(2);

        catalog.Recommendation = [SecondTitleId, Guid.NewGuid()];
        var discovery = await new GetPersonalizedDiscoveryHandler(store, catalog, new FakeRecommendation()).Handle(new(UserId, "vi"), CancellationToken.None);
        discovery.Value.ContinueWatching.Should().ContainSingle();
        discovery.Value.Recommended.Should().ContainSingle().Which.Slug.Should().Be("second");
    }

    [Fact]
    public async Task Engagement_command_handlers_cover_success_and_not_found_paths()
    {
        var store = new FakeLibraryStore();
        var catalog = new FakeLibraryCatalog { TitleId = FirstTitleId, Playable = new(FirstTitleId, Guid.NewGuid(), 1) };
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero));
        (await new SaveTitleHandler(store, catalog, time).Handle(new(UserId, "first"), default)).Value.Should().BeTrue();
        (await new SaveTitleHandler(store, new FakeLibraryCatalog(), time).Handle(new(UserId, "missing"), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new RemoveSavedTitleHandler(store, catalog).Handle(new(UserId, "first"), default)).Value.Should().BeTrue();
        (await new RemoveSavedTitleHandler(store, new FakeLibraryCatalog()).Handle(new(UserId, "missing"), default)).FirstError.Code.Should().Be("engagement.saved.not_found");
        (await new RecordWatchProgressHandler(store, catalog, time).Handle(new(UserId, "first", 1, -2), default)).Value.Should().BeTrue();
        (await new RecordWatchProgressHandler(store, new FakeLibraryCatalog(), time).Handle(new(UserId, "missing", null, 2), default)).FirstError.Code.Should().Be("catalog.playable.not_found");

        var analytics = new FakeViewAnalyticsStore { View = new(3, true) };
        (await new RecordTitleViewHandler(analytics, catalog, time).Handle(new("first", UserId, "session", 1), default)).Value.ViewCount.Should().Be(3);
        (await new RecordTitleViewHandler(analytics, new FakeLibraryCatalog(), time).Handle(new("missing", null, "session", null), default)).FirstError.Code.Should().Be("catalog.title.not_found");
    }

    [Fact]
    public async Task Top_and_review_handlers_normalize_locale_and_calculate_average()
    {
        var catalog = new FakeLibraryCatalog { TitleId = FirstTitleId, Titles = new Dictionary<Guid, LibraryTitle> { [FirstTitleId] = FirstTitle } };
        var cache = new FakeTopCache();
        var analytics = new FakeViewAnalyticsStore { Top = [new(FirstTitleId, 9), new(Guid.NewGuid(), 4)] };
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero));
        var top = await new GetTopTitlesHandler(analytics, catalog, cache, time).Handle(new(TopPeriod.Week, "en-US", 5), default);
        top.Value.Should().ContainSingle().Which.Views.Should().Be(9);
        cache.Locale.Should().Be("en");

        var reviews = new FakeReviewStore { Reviews = [new(Guid.NewGuid(), "A", 8, "good", DateTimeOffset.UtcNow), new(Guid.NewGuid(), "B", 9, null, DateTimeOffset.UtcNow)] };
        var reviewResult = await new GetTitleReviewsHandler(reviews, catalog).Handle(new("first"), default);
        reviewResult.Value.AverageRating.Should().Be(8.5);
        (await new GetTitleReviewsHandler(reviews, new FakeLibraryCatalog()).Handle(new("missing"), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new SubmitTitleReviewHandler(reviews, catalog, time).Handle(new(UserId, "A", "first", 9, "  hello  "), default)).Value.Should().BeTrue();
        reviews.StoredReview!.Comment.Should().Be("hello");
        reviews.StoredReview.CreatedAt.Should().Be(time.GetUtcNow());
        (await new SubmitTitleReviewHandler(reviews, new FakeLibraryCatalog(), time).Handle(new(UserId, "A", "missing", 9, null), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new RemoveTitleReviewHandler(reviews, catalog).Handle(new(UserId, "first"), default)).Value.Should().BeTrue();
        (await new RemoveTitleReviewHandler(new FakeReviewStore(), new FakeLibraryCatalog { TitleId = FirstTitleId }).Handle(new(UserId, "first"), default)).FirstError.Code.Should().Be("engagement.review.not_found");
    }

    [Fact]
    public async Task Catalog_search_identity_and_assistant_handlers_cover_their_branches()
    {
        var catalogStore = new FakeCatalogStore
        {
            List = new([new("first", "First", "Drama", 2026, "movie", "poster")], 1),
            Detail = new("first", "First", "Synopsis", "Drama", 2026, "movie", "poster", 100, 3),
            Playback = new("first", "First", false, []),
            Home = new(new("first", "First", "Drama", 2026, "movie", "poster"), [])
        };
        (await new ListTitlesHandler(catalogStore).Handle(new(" q ", "Drama", null, null, null, null, 1, 30, "en-US"), default)).Value.Total.Should().Be(1);
        (await new GetTitleHandler(catalogStore).Handle(new("first", null), default)).Value.Slug.Should().Be("first");
        (await new GetTitleHandler(new FakeCatalogStore()).Handle(new("missing", null), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new GetGenresHandler(catalogStore).Handle(new(), default)).Value.Should().ContainSingle();
        (await new GetPlaybackHandler(catalogStore).Handle(new("first", null), default)).Value.Slug.Should().Be("first");
        (await new GetPlaybackHandler(new FakeCatalogStore()).Handle(new("missing", null), default)).FirstError.Code.Should().Be("catalog.playback.not_found");
        (await new GetHomeHandler(catalogStore).Handle(new(null), default)).Value.Hero.Slug.Should().Be("first");
        var schedule = await new GetScheduleHandler(catalogStore, new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero)))
            .Handle(new(null, "en-US"), default);
        schedule.Value.WeekStart.Should().Be(new DateOnly(2026, 8, 31));

        var searchStore = new FakeSearchStore { Result = new([new("first", "First", "Drama", 2026, "movie", "poster")], 1) };
        (await new SearchCatalogHandler(searchStore).Handle(new("a", null, null, null), default)).Value.Total.Should().Be(1);

        var assistantStore = new FakeAssistantStore { Results = [new(new("first", "First", "Drama", 2026, "movie", "poster"), "reason")] };
        var assistant = await new AskCatalogAssistantHandler(assistantStore, new FakeAssistantGenerator()).Handle(new(UserId, "sad", null), default);
        assistant.Value.Suggestions.Should().ContainSingle();
        assistantStore.UserId.Should().Be(UserId);

        var verifier = new FakeVerifier { Identity = new("sub", "a@test", "A", null) };
        var repo = new FakeUserRepository();
        var allowlist = new FakeAllowlist();
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero));
        (await new SignInWithGoogleHandler(verifier, repo, allowlist, time).Handle(new("credential"), default)).Value.Email.Should().Be("a@test");
        verifier.Identity = null;
        (await new SignInWithGoogleHandler(verifier, repo, allowlist, time).Handle(new("bad"), default)).FirstError.Code.Should().Be("auth.google.invalid_credential");
    }

    [Fact]
    public async Task Validators_and_validation_behavior_return_errors_or_call_next()
    {
        new ListTitlesValidator().Validate(new ListTitlesQuery(new string('x', 201), null, null, null, null, null, 1, 30, null)).IsValid.Should().BeFalse();
        new RecordTitleViewValidator().Validate(new RecordTitleViewCommand("", null, "", 0)).IsValid.Should().BeFalse();
        new SubmitTitleReviewValidator().Validate(new SubmitTitleReviewCommand(UserId, "", "", 11, new string('x', 2001))).IsValid.Should().BeFalse();
        new AskCatalogAssistantValidator().Validate(new AskCatalogAssistantQuery(UserId, "", null)).IsValid.Should().BeFalse();

        var valid = new ValidationBehavior<RecordTitleViewCommand, ViewRecordedResponse>([new RecordTitleViewValidator()]);
        var called = false;
        var success = await valid.Handle(new RecordTitleViewCommand("slug", null, "session", null), _ =>
        {
            called = true;
            return Task.FromResult<ErrorOr<ViewRecordedResponse>>(new ViewRecordedResponse(1, true));
        }, default);
        called.Should().BeTrue();
        success.Value.Counted.Should().BeTrue();
        var invalid = await valid.Handle(new RecordTitleViewCommand("", null, "", -1), _ => throw new InvalidOperationException(), default);
        invalid.IsError.Should().BeTrue();
    }

    [Fact]
    public void Domain_entities_and_contract_records_expose_values_and_localize_synopsis()
    {
        var title = ZMovie.Domain.Catalog.Title.Create(
            new ZMovie.Domain.Catalog.TitleId(FirstTitleId),
            ZMovie.Domain.Catalog.TitleSlug.Parse("s"),
            new ZMovie.Domain.Catalog.LocalizedText("V", "E"),
            new ZMovie.Domain.Catalog.LocalizedText("VS", "ES"),
            "G",
            ZMovie.Domain.Catalog.ReleaseYear.FromInt(2026),
            ZMovie.Domain.Catalog.TitleType.Movie,
            "P",
            ZMovie.Domain.Catalog.Runtime.FromMinutes(90),
            false,
            DateTimeOffset.UtcNow);
        title.LocalizedSynopsis("en").Should().Be("ES");
        title.LocalizedSynopsis("vi").Should().Be("VS");
        _ = ZMovie.Domain.Catalog.Genre.Create(new ZMovie.Domain.Catalog.GenreId(Guid.NewGuid()), "g", "G", DateTimeOffset.UtcNow);
        _ = ZMovie.Domain.Catalog.Episode.Create(new ZMovie.Domain.Catalog.EpisodeId(Guid.NewGuid()), new ZMovie.Domain.Catalog.TitleId(FirstTitleId), 1, "1", "url");
        _ = ZMovie.Domain.Engagement.SavedTitle.Create(new EngagementUserId(UserId), new EngagementTitleId(FirstTitleId), DateTimeOffset.UtcNow).SavedAt;
        _ = ZMovie.Domain.Engagement.WatchProgress.Record(new EngagementUserId(UserId), new EngagementPlayableId(Guid.NewGuid()), new EngagementTitleId(FirstTitleId), 1, WatchPosition.FromSeconds(10), DateTimeOffset.UtcNow).UpdatedAt;
        _ = ZMovie.Domain.Analytics.TitleViewEvent.Record(ViewEventId.New(), new AnalyticsTitleId(FirstTitleId), null, null, "s", DateTimeOffset.UtcNow).ViewedAt;
        _ = ZMovie.Domain.Identity.User.Create(new ZMovie.Domain.Identity.UserId(UserId), new ZMovie.Domain.Identity.ExternalIdentity("sub"), "e", "n", null, ZMovie.Domain.Identity.Role.Member, DateTimeOffset.UtcNow).LastSignedInAt;
        _ = new GoogleIdentity("s", "e", "n", null);
        _ = new AuthenticatedUser(UserId, "e", "n", null, ZMovie.Domain.Identity.Role.MemberName);
        _ = new UserLibraryResponse([], []);
        _ = new PersonalizedDiscoveryResponse([], []);
        _ = new AssistantContextResponse([]);
        Locale.Normalize("en-GB").Should().Be("en");
        Locale.Normalize(null).Should().Be("vi");
    }

    private sealed class FakeLibraryStore : IUserLibraryQueries, ISavedTitleRepository, IWatchProgressRepository
    {
        public IReadOnlyList<SavedTitleEntry> Saved { get; set; } = [];
        public IReadOnlyList<WatchProgressEntry> History { get; set; } = [];
        public SavedTitle? StoredSavedTitle { get; private set; }
        public WatchProgress? StoredWatchProgress { get; private set; }
        public bool HasSaved { get; set; } = true;

        public Task<IReadOnlyList<SavedTitleEntry>> ListSavedAsync(EngagementUserId userId, CancellationToken ct) => Task.FromResult(Saved);
        public Task<IReadOnlyList<WatchProgressEntry>> ListHistoryAsync(EngagementUserId userId, CancellationToken ct) => Task.FromResult(History);

        public Task<SavedTitle?> FindAsync(EngagementUserId userId, EngagementTitleId titleId, CancellationToken ct) =>
            Task.FromResult(HasSaved ? StoredSavedTitle ?? SavedTitle.Create(userId, titleId, DateTimeOffset.UtcNow) : null);

        public void Add(SavedTitle savedTitle) => StoredSavedTitle = savedTitle;
        public void Remove(SavedTitle savedTitle) { StoredSavedTitle = null; HasSaved = false; }

        public Task<WatchProgress?> FindAsync(EngagementUserId userId, EngagementPlayableId playableId, CancellationToken ct) =>
            Task.FromResult(StoredWatchProgress);

        public void Add(WatchProgress progress) => StoredWatchProgress = progress;

        public Task RemoveByTitleAsync(EngagementUserId userId, EngagementTitleId titleId, CancellationToken ct)
        {
            StoredWatchProgress = null;
            return Task.CompletedTask;
        }

        public Task ClearAllAsync(EngagementUserId userId, CancellationToken ct)
        {
            StoredWatchProgress = null;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeViewAnalyticsStore : IViewEventRepository, IViewAnalyticsQueries
    {
        public IReadOnlyList<TopViewCount> Top { get; set; } = [];
        public ViewRecordedResponse View { get; set; } = new(0, false);

        public Task<ViewRecordedResponse> RecordViewWithLockAsync(
            AnalyticsTitleId titleId,
            int? episodeNumber,
            AnalyticsUserId? userId,
            string sessionId,
            DateTimeOffset occurredAt,
            CancellationToken ct) => Task.FromResult(View);

        public Task<long> GetViewCountAsync(AnalyticsTitleId titleId, CancellationToken ct) => Task.FromResult(View.ViewCount);

        public Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, DateTimeOffset now, CancellationToken ct) => Task.FromResult(Top);
    }

    private sealed class FakeLibraryCatalog : ILibraryCatalogReader
    {
        public Guid? TitleId { get; set; }
        public PlayableReference? Playable { get; set; }
        public IReadOnlyDictionary<Guid, LibraryTitle> Titles { get; set; } = new Dictionary<Guid, LibraryTitle>();
        public IReadOnlyList<RecommendationCandidate> Candidates { get; set; } = [];
        public IReadOnlyList<Guid> Recommendation { get; set; } = [];
        public Task<Guid?> FindTitleIdAsync(string slug, CancellationToken ct) => Task.FromResult(TitleId);
        public Task<PlayableReference?> FindPlayableAsync(string slug, int? episodeNumber, CancellationToken ct) => Task.FromResult(Playable);
        public Task<IReadOnlyDictionary<Guid, LibraryTitle>> GetTitlesAsync(IEnumerable<Guid> titleIds, string locale, CancellationToken ct) => Task.FromResult(Titles);
        public Task<IReadOnlyList<LibraryTitle>> GetDiscoveryTitlesAsync(string locale, CancellationToken ct) => Task.FromResult<IReadOnlyList<LibraryTitle>>(Titles.Values.ToList());
        public Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationCandidatesAsync(string locale, CancellationToken ct) => Task.FromResult(Candidates);
    }

    private sealed class FakeRecommendation : IRecommendationEngine
    {
        public IReadOnlyList<Guid> Recommend(IReadOnlyList<RecommendationCandidate> candidates, IReadOnlyList<RecommendationSeed> profile, IReadOnlySet<Guid> excludedTitleIds, int limit) => [SecondTitleId, Guid.NewGuid()];
    }

    private sealed class FakeTopCache : ITopTitlesResponseCache
    {
        public string? Locale { get; private set; }
        public async Task<IReadOnlyList<TopTitleResponse>> GetOrCreateAsync(TopPeriod period, string locale, int limit, Func<CancellationToken, Task<IReadOnlyList<TopTitleResponse>>> factory, CancellationToken ct) { Locale = locale; return await factory(ct); }
    }

    private sealed class FakeReviewStore : IReviewRepository, IReviewQueries
    {
        public IReadOnlyList<ReviewEntry> Reviews { get; set; } = [];
        public Review? StoredReview { get; private set; }

        public Task<IReadOnlyList<ReviewEntry>> ListByTitleAsync(EngagementTitleId titleId, CancellationToken ct) => Task.FromResult(Reviews);

        public Task<Review?> FindByTitleAndUserAsync(EngagementTitleId titleId, EngagementUserId userId, CancellationToken ct) =>
            Task.FromResult(StoredReview is not null && StoredReview.TitleId == titleId && StoredReview.UserId == userId
                ? StoredReview
                : null);

        public Task<Review?> FindByIdAsync(ReviewId reviewId, CancellationToken ct) =>
            Task.FromResult(StoredReview?.Id == reviewId ? StoredReview : null);

        public void Add(Review review) => StoredReview = review;

        public void Remove(Review review)
        {
            if (ReferenceEquals(StoredReview, review)) StoredReview = null;
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeCatalogStore : ICatalogReadStore
    {
        public TitleListResponse List { get; set; } = new([], 0);
        public TitleDetail? Detail { get; set; }
        public PlaybackResponse? Playback { get; set; }
        public HomeResponse? Home { get; set; }
        public ScheduleResponse Schedule { get; set; } = new(new DateOnly(2026, 8, 31), []);
        public PeopleResponse People { get; set; } = new([], 0);
        public PersonDetail? Person { get; set; }
        public Task<TitleListResponse> ListAsync(string? query, string? genre, string? country, int? year, string? type, string? sort, int page, int pageSize, string locale, CancellationToken ct) => Task.FromResult(List);
        public Task<TitleDetail?> GetAsync(string slug, string locale, CancellationToken ct) => Task.FromResult(Detail);
        public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>(["Drama"]);
        public Task<PlaybackResponse?> GetPlaybackAsync(string slug, string locale, CancellationToken ct) => Task.FromResult(Playback);
        public Task<HomeResponse?> GetHomeAsync(string locale, CancellationToken ct) => Task.FromResult(Home);
        public Task<ScheduleResponse> GetScheduleAsync(DateOnly weekStart, string locale, CancellationToken ct) => Task.FromResult(Schedule with { WeekStart = weekStart });
        public Task<PeopleResponse> ListPeopleAsync(string? query, int page, int pageSize, CancellationToken ct) => Task.FromResult(People);
        public Task<PersonDetail?> GetPersonAsync(string slug, string locale, CancellationToken ct) => Task.FromResult(Person);
    }

    private sealed class FakeSearchStore : ISearchCatalogStore
    {
        public TitleListResponse Result { get; set; } = new([], 0);
        public Task<TitleListResponse> SearchAsync(string query, string? type, string? genre, string locale, CancellationToken ct) => Task.FromResult(Result);
    }

    private sealed class FakeAssistantStore : ICatalogAssistantStore
    {
        public IReadOnlyList<AssistantCatalogTitle> Results { get; set; } = [];
        public Guid? UserId { get; private set; }
        public Task<IReadOnlyList<AssistantCatalogTitle>> SearchAsync(Guid userId, string message, string locale, int limit, CancellationToken ct) { UserId = userId; return Task.FromResult(Results); }
    }

    private sealed class FakeAssistantGenerator : IAssistantTextGenerator
    {
        public Task<string?> GenerateAsync(AssistantGenerationRequest request, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    private sealed class FakeVerifier : IGoogleIdentityVerifier
    {
        public GoogleIdentity? Identity { get; set; }
        public Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct) => Task.FromResult(Identity);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public ZMovie.Domain.Identity.User? StoredUser { get; set; }

        public Task<ZMovie.Domain.Identity.User?> FindByIdAsync(ZMovie.Domain.Identity.UserId id, CancellationToken ct) =>
            Task.FromResult(StoredUser?.Id == id ? StoredUser : null);

        public Task<ZMovie.Domain.Identity.User?> FindByExternalIdentityAsync(ZMovie.Domain.Identity.ExternalIdentity externalIdentity, CancellationToken ct) =>
            Task.FromResult(StoredUser?.ExternalIdentity == externalIdentity ? StoredUser : null);

        public Task<ZMovie.Domain.Identity.User?> FindByLoginAsync(string login, CancellationToken ct) =>
            Task.FromResult(StoredUser is not null && (StoredUser.Username == login || StoredUser.Email == login) ? StoredUser : null);

        public Task<ZMovie.Domain.Identity.User?> FindByEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(StoredUser?.Email == email ? StoredUser : null);

        public void Add(ZMovie.Domain.Identity.User user) => StoredUser = user;

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<ZMovie.Application.Identity.SetRoleOutcome> ChangeRoleWithLastAdminGuardAsync(ZMovie.Domain.Identity.UserId userId, ZMovie.Domain.Identity.Role newRole, bool guardLastAdmin, CancellationToken ct) =>
            Task.FromResult(ZMovie.Application.Identity.SetRoleOutcome.Updated);
    }

    private sealed class FakeAllowlist : IAdminAllowlist
    {
        public bool IsAllowlisted(string email) => false;
    }
}
