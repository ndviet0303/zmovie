using FluentAssertions;
using FluentValidation;
using MediatR;
using ErrorOr;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Application.Identity;
using ZMovie.Application.Search;
using Xunit;

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
        (await new SaveTitleHandler(store, catalog).Handle(new(UserId, "first"), default)).Value.Should().BeTrue();
        (await new SaveTitleHandler(store, new FakeLibraryCatalog()).Handle(new(UserId, "missing"), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new RemoveSavedTitleHandler(store, catalog).Handle(new(UserId, "first"), default)).Value.Should().BeTrue();
        (await new RemoveSavedTitleHandler(store, new FakeLibraryCatalog()).Handle(new(UserId, "missing"), default)).FirstError.Code.Should().Be("engagement.saved.not_found");
        (await new RecordWatchProgressHandler(store, catalog).Handle(new(UserId, "first", 1, -2), default)).Value.Should().BeTrue();
        (await new RecordWatchProgressHandler(store, new FakeLibraryCatalog()).Handle(new(UserId, "missing", null, 2), default)).FirstError.Code.Should().Be("catalog.playable.not_found");
        store.View = new(3, true);
        (await new RecordTitleViewHandler(store, catalog).Handle(new("first", UserId, "session", 1), default)).Value.ViewCount.Should().Be(3);
        (await new RecordTitleViewHandler(store, new FakeLibraryCatalog()).Handle(new("missing", null, "session", null), default)).FirstError.Code.Should().Be("catalog.title.not_found");
    }

    [Fact]
    public async Task Top_and_review_handlers_normalize_locale_and_calculate_average()
    {
        var catalog = new FakeLibraryCatalog { TitleId = FirstTitleId, Titles = new Dictionary<Guid, LibraryTitle> { [FirstTitleId] = FirstTitle } };
        var cache = new FakeTopCache();
        var analytics = new FakeLibraryStore { Top = [new(FirstTitleId, 9), new(Guid.NewGuid(), 4)] };
        var top = await new GetTopTitlesHandler(analytics, catalog, cache).Handle(new(TopPeriod.Week, "en-US", 5), default);
        top.Value.Should().ContainSingle().Which.Views.Should().Be(9);
        cache.Locale.Should().Be("en");

        var reviews = new FakeReviewStore { Reviews = [new(Guid.NewGuid(), "A", 8, "good", DateTimeOffset.UtcNow), new(Guid.NewGuid(), "B", 9, null, DateTimeOffset.UtcNow)] };
        var reviewResult = await new GetTitleReviewsHandler(reviews, catalog).Handle(new("first"), default);
        reviewResult.Value.AverageRating.Should().Be(8.5);
        (await new GetTitleReviewsHandler(reviews, new FakeLibraryCatalog()).Handle(new("missing"), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new SubmitTitleReviewHandler(reviews, catalog).Handle(new(UserId, "A", "first", 9, "  hello  "), default)).Value.Should().BeTrue();
        (await new SubmitTitleReviewHandler(reviews, new FakeLibraryCatalog()).Handle(new(UserId, "A", "missing", 9, null), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        reviews.RemoveResult = true;
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
        (await new ListTitlesHandler(catalogStore).Handle(new(" q ", "Drama", "en-US"), default)).Value.Total.Should().Be(1);
        (await new GetTitleHandler(catalogStore).Handle(new("first", null), default)).Value.Slug.Should().Be("first");
        (await new GetTitleHandler(new FakeCatalogStore()).Handle(new("missing", null), default)).FirstError.Code.Should().Be("catalog.title.not_found");
        (await new GetGenresHandler(catalogStore).Handle(new(), default)).Value.Should().Contain("Drama");
        (await new GetPlaybackHandler(catalogStore).Handle(new("first", null), default)).Value.Slug.Should().Be("first");
        (await new GetPlaybackHandler(new FakeCatalogStore()).Handle(new("missing", null), default)).FirstError.Code.Should().Be("catalog.playback.not_found");
        (await new GetHomeHandler(catalogStore).Handle(new(null), default)).Value.Hero.Slug.Should().Be("first");
        (await new GetHomeHandler(new FakeCatalogStore()).Handle(new(null), default)).FirstError.Code.Should().Be("catalog.home.unavailable");

        var searchStore = new FakeSearchStore { Result = new([], 0) };
        (await new SearchCatalogHandler(searchStore).Handle(new("  hello  ", null, null, null), default)).Value.Total.Should().Be(0);
        var assistantStore = new FakeAssistantStore { Results = [new(new("first", "First", "Drama", 2026, "movie", "poster"), "Synopsis")] };
        var assistantContext = await new GetAssistantContextHandler(assistantStore).Handle(new(UserId, "  drama ", "en"), default);
        assistantContext.Value.Matches.Should().ContainSingle();
        var assistant = await new AskCatalogAssistantHandler(assistantStore, new FakeAssistantGenerator()).Handle(new(UserId, "  drama ", "en"), default);
        assistant.Value.Message.Should().Contain("I found 1");
        assistantStore.Results = [];
        (await new AskCatalogAssistantHandler(assistantStore, new FakeAssistantGenerator()).Handle(new(UserId, "drama", "vi"), default)).Value.Message.Should().Contain("chưa tìm");
        assistantStore.Results = [new(new("comfort", "Warm Friends", "Family", 2026, "movie", "poster"), "A gentle story")];
        (await new AskCatalogAssistantHandler(assistantStore, new FakeAssistantGenerator()).Handle(new(UserId, "hôm nay tôi buồn", "vi"), default)).Value.Message.Should().Contain("nhẹ nhàng");

        var verifier = new FakeVerifier { Identity = new("subject", "a@test", "A", null) };
        var users = new FakeUserIdentityStore { User = new(UserId, "a@test", "A", null) };
        (await new SignInWithGoogleHandler(verifier, users).Handle(new("credential"), default)).Value.Id.Should().Be(UserId);
        verifier.Identity = null;
        (await new SignInWithGoogleHandler(verifier, users).Handle(new("bad"), default)).FirstError.Code.Should().Be("auth.google.invalid_credential");
    }

    [Fact]
    public async Task Validators_and_validation_behavior_return_errors_or_call_next()
    {
        new ListTitlesValidator().Validate(new ListTitlesQuery(new string('x', 201), null, null)).IsValid.Should().BeFalse();
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
        var title = new ZMovie.Domain.Catalog.CatalogTitle { Slug = "s", EnglishTitle = "E", VietnameseTitle = "V", EnglishSynopsis = "ES", VietnameseSynopsis = "VS", Genre = "G", Type = "movie", PosterUrl = "P" };
        title.LocalizedSynopsis("en").Should().Be("ES");
        title.LocalizedSynopsis("vi").Should().Be("VS");
        _ = new ZMovie.Domain.Catalog.CatalogGenre { Slug = "g", Name = "G" };
        _ = new ZMovie.Domain.Catalog.CatalogEpisode { Name = "1", HlsUrl = "url" };
        _ = new ZMovie.Domain.Engagement.SavedTitle { UserId = UserId, TitleId = FirstTitleId }.SavedAt;
        _ = new ZMovie.Domain.Engagement.WatchProgress { UserId = UserId, PlayableId = Guid.NewGuid(), TitleId = FirstTitleId }.UpdatedAt;
        _ = new ZMovie.Domain.Engagement.TitleViewEvent { TitleId = FirstTitleId, SessionId = "s" }.ViewedAt;
        _ = new ZMovie.Domain.Engagement.TitleReview { TitleId = FirstTitleId, UserId = UserId, AuthorName = "A", Rating = 8 }.UpdatedAt;
        _ = new ZMovie.Domain.Identity.ZMovieUser { GoogleSubject = "sub", Email = "e", DisplayName = "n" }.LastSignedInAt;
        _ = new GoogleIdentity("s", "e", "n", null);
        _ = new AuthenticatedUser(UserId, "e", "n", null);
        _ = new UserLibraryResponse([], []);
        _ = new PersonalizedDiscoveryResponse([], []);
        _ = new AssistantContextResponse([]);
        Locale.Normalize("en-GB").Should().Be("en");
        Locale.Normalize(null).Should().Be("vi");
    }

    private sealed class FakeLibraryStore : IUserLibraryStore, IViewAnalyticsStore
    {
        public IReadOnlyList<SavedTitleEntry> Saved { get; set; } = [];
        public IReadOnlyList<WatchProgressEntry> History { get; set; } = [];
        public IReadOnlyList<TopViewCount> Top { get; set; } = [];
        public ViewRecordedResponse View { get; set; } = new(0, false);
        public Task<IReadOnlyList<SavedTitleEntry>> GetSavedAsync(Guid userId, CancellationToken ct) => Task.FromResult(Saved);
        public Task<IReadOnlyList<WatchProgressEntry>> GetHistoryAsync(Guid userId, CancellationToken ct) => Task.FromResult(History);
        public Task SaveAsync(Guid userId, Guid titleId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> RemoveAsync(Guid userId, Guid titleId, CancellationToken ct) => Task.FromResult(true);
        public Task RecordProgressAsync(Guid userId, PlayableReference playable, double progressSeconds, CancellationToken ct) => Task.CompletedTask;
        public Task<ViewRecordedResponse> RecordAsync(Guid titleId, Guid? userId, string sessionId, int? episodeNumber, CancellationToken ct) => Task.FromResult(View);
        public Task<long> GetViewCountAsync(Guid titleId, CancellationToken ct) => Task.FromResult(View.ViewCount);
        public Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, CancellationToken ct) => Task.FromResult(Top);
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

    private sealed class FakeReviewStore : ITitleReviewStore
    {
        public IReadOnlyList<ReviewEntry> Reviews { get; set; } = [];
        public bool RemoveResult { get; set; }
        public Task<IReadOnlyList<ReviewEntry>> GetAsync(Guid titleId, CancellationToken ct) => Task.FromResult(Reviews);
        public Task UpsertAsync(Guid titleId, Guid userId, string authorName, int rating, string? comment, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> RemoveReviewAsync(Guid titleId, Guid userId, CancellationToken ct) => Task.FromResult(RemoveResult);
    }

    private sealed class FakeCatalogStore : ICatalogReadStore
    {
        public TitleListResponse List { get; set; } = new([], 0);
        public TitleDetail? Detail { get; set; }
        public PlaybackResponse? Playback { get; set; }
        public HomeResponse? Home { get; set; }
        public Task<TitleListResponse> ListAsync(string? query, string? genre, string locale, CancellationToken ct) => Task.FromResult(List);
        public Task<TitleDetail?> GetAsync(string slug, string locale, CancellationToken ct) => Task.FromResult(Detail);
        public Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>(["Drama"]);
        public Task<PlaybackResponse?> GetPlaybackAsync(string slug, string locale, CancellationToken ct) => Task.FromResult(Playback);
        public Task<HomeResponse?> GetHomeAsync(string locale, CancellationToken ct) => Task.FromResult(Home);
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

    private sealed class FakeUserIdentityStore : IUserIdentityStore
    {
        public AuthenticatedUser User { get; set; } = default!;
        public Task<AuthenticatedUser> UpsertGoogleUserAsync(GoogleIdentity identity, CancellationToken ct) => Task.FromResult(User);
    }
}
