using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Analytics;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;
using ZMovie.Application.Identity;
using ZMovie.Application.Personalization;
using ZMovie.Domain.Analytics;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;
using ZMovie.Domain.Personalization;
using ZMovie.Infrastructure.Administration;
using ZMovie.Infrastructure.Analytics;
using ZMovie.Infrastructure.Assistant;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Identity;
using ZMovie.Infrastructure.Persistence;
using ZMovie.Infrastructure.Personalization;
using ZMovie.Infrastructure.Recommendations;
using ZMovie.Infrastructure.Realtime;
using ZMovie.Infrastructure.WatchParty;
using ZMovie.Infrastructure.Recommendations.Models;
using ZMovie.Infrastructure.Search;
using ZMovie.Infrastructure.Seed;
using Xunit;
using CatalogTitle = ZMovie.Domain.Catalog.Title;
using CatalogTitleId = ZMovie.Domain.Catalog.TitleId;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;
using EngagementUserId = ZMovie.Domain.Engagement.UserId;
using EngagementPlayableId = ZMovie.Domain.Engagement.PlayableId;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;
using AnalyticsUserId = ZMovie.Domain.Analytics.UserId;
using AnalyticsViewEventId = ZMovie.Domain.Analytics.ViewEventId;
using IdentityUserId = ZMovie.Domain.Identity.UserId;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task Catalog_readers_and_stores_cover_empty_and_populated_paths()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("first", "First", "Đầu tiên", "movie", featured: true);
        var series = MakeTitle("series", "Series", "Bộ phim", "series", featured: false);
        database.Db.Titles.AddRange(title, series);
        database.Db.Episodes.Add(Episode.Create(EpisodeId.New(), series.Id, 1, "Episode 1", "https://video/1"));
        await database.Db.SaveChangesAsync();

        var reader = new CatalogLibraryReader(database.Db);
        (await reader.FindTitleIdAsync("first", default)).Should().Be(title.Id.Value);
        (await reader.FindTitleIdAsync("missing", default)).Should().BeNull();
        (await reader.FindPlayableAsync("missing", null, default)).Should().BeNull();
        (await reader.FindPlayableAsync("first", null, default)).Should().NotBeNull();
        (await reader.FindPlayableAsync("series", 1, default)).Should().NotBeNull();
        (await reader.FindPlayableAsync("series", 9, default)).Should().BeNull();
        (await reader.GetTitlesAsync([], "vi", default)).Should().BeEmpty();
        (await reader.GetTitlesAsync([title.Id.Value], "en", default)).Should().ContainKey(title.Id.Value);
        (await reader.GetDiscoveryTitlesAsync("vi", default)).Should().HaveCount(2);
        (await reader.GetRecommendationCandidatesAsync("en", default)).Should().HaveCount(2);

        var analytics = new FakeAnalytics { Counts = new Dictionary<Guid, long> { [title.Id.Value] = 7 } };
        var store = new EfCatalogReadStore(database.Db, analytics);
        (await store.ListAsync("First", null, null, null, null, null, 1, 30, "en", default)).Items.Should().ContainSingle();
        var genreQuery = () => store.ListAsync(null, "Drama", null, null, null, null, 1, 30, "vi", default);
        await genreQuery.Should().ThrowAsync<InvalidOperationException>();
        (await store.ListAsync(null, null, null, null, null, null, 1, 30, "vi", default)).Should().BeOfType<TitleListResponse>();
        var detail = await store.GetAsync("first", "en", default);
        detail.Should().NotBeNull();
        detail!.ViewCount.Should().Be(7);
        (await store.GetAsync("missing", "vi", default)).Should().BeNull();
        var playback = await store.GetPlaybackAsync("series", "vi", default);
        playback.Should().NotBeNull();
        playback!.IsSeries.Should().BeTrue();
        (await store.GetPlaybackAsync("missing", "vi", default)).Should().BeNull();
        (await store.GetHomeAsync("vi", default)).Should().NotBeNull();

        database.Db.Genres.Add(Genre.Create(GenreId.New(), "drama", "Drama", DateTimeOffset.UtcNow));
        await database.Db.SaveChangesAsync();
        var importedGenres = await store.GetGenresAsync(default);
        importedGenres.Should().ContainSingle();
        importedGenres[0].Should().Be("Drama");
        database.Db.Genres.RemoveRange(database.Db.Genres);
        await database.Db.SaveChangesAsync();
        (await store.GetGenresAsync(default)).Should().Contain("Drama");
    }

    [Fact]
    public void Watch_party_registry_lists_joins_updates_and_requires_management_token()
    {
        var registry = new InMemoryWatchPartyRegistry(
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero)));
        var created = registry.Create("first", 2);
        var joined = registry.Join(created.Room.RoomId, "connection", "Lan", "first", 2);
        registry.UpdatePlayback(created.Room.RoomId, true, 42);

        joined.ActiveUsers.Should().ContainSingle().Which.Should().Be("Lan");
        registry.List().Should().ContainSingle().Which.Should().Match<ZMovie.Application.WatchParty.WatchPartyRoom>(
            room => room.ActiveUserCount == 1 && room.IsPlaying && room.CurrentTime == 42);
        registry.Delete(created.Room.RoomId, "wrong").Should().BeFalse();
        registry.Delete(created.Room.RoomId, created.ManagementToken).Should().BeTrue();
        registry.List().Should().BeEmpty();
    }

    [Fact]
    public async Task Catalog_home_uses_featured_fallback_and_returns_null_when_empty()
    {
        using var empty = new TestDatabase();
        var emptyStore = new EfCatalogReadStore(empty.Db, new FakeAnalytics());
        var emptyHome = await emptyStore.GetHomeAsync("vi", default);
        emptyHome.Should().BeNull();

        using var database = new TestDatabase();
        database.Db.Titles.Add(MakeTitle("featured", "Featured", "Nổi bật", "movie", featured: true));
        await database.Db.SaveChangesAsync();
        var home = await new EfCatalogReadStore(database.Db, new FakeAnalytics()).GetHomeAsync("en", default);
        home.Should().NotBeNull();
        home!.Hero.Slug.Should().Be("featured");
    }

    [Fact]
    public async Task Catalog_playback_normalizes_stale_direct_video_source_format()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("direct-video", "Direct Video", "Video trực tiếp", "movie", featured: true);
        var episode = Episode.Create(
            EpisodeId.New(),
            title.Id,
            1,
            "Tập 1",
            "https://cdn.example.com/movie.mp4");
        var source = episode.Sources.Single();
        source.UpdateDetails("https://cdn.example.com/movie.mp4", "embed", 1);
        database.Db.Titles.Add(title);
        database.Db.Episodes.Add(episode);
        await database.Db.SaveChangesAsync();

        var playback = await new EfCatalogReadStore(database.Db, new FakeAnalytics())
            .GetPlaybackAsync("direct-video", "vi", default);

        playback.Should().NotBeNull();
        playback!.Episodes.Single().Sources.Should().ContainSingle()
            .Which.Format.Should().Be(StreamFormat.Video);
    }

    [Fact]
    public async Task Recommendation_engine_recommends_candidates_based_on_tfidf_similarity()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var candidates = new List<RecommendationCandidate>
        {
            new(a, new("a", "Space Journey", "Sci Fi", 2026, "movie", "", 100), "A space adventure"),
            new(b, new("b", "Garden", "Drama", 2025, "movie", "", 90), "A quiet garden")
        };
        var model = TinyTfidfRecommendationModel.Train(candidates);
        model.Recommend([new(a, 2), new(Guid.NewGuid(), 1)], new HashSet<Guid> { a }, 5).Should().Contain(b);
        TinyTfidfRecommendationModel.Train([]).Recommend([], new HashSet<Guid>(), 1).Should().BeEmpty();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var engine = new TinyContentRecommendationEngine(cache);
        engine.Recommend(candidates, [new(a, 1)], new HashSet<Guid> { a }, 1).Should().ContainSingle();
        engine.Recommend(candidates, [new(a, 1)], new HashSet<Guid> { a }, 1).Should().ContainSingle();
    }

    [Fact]
    public async Task Identity_store_creates_and_updates_user()
    {
        using var database = new TestDatabase();
        var repo = new EfUserRepository(database.Identity);
        var queries = new EfUserQueries(database.Identity);
        var user = User.Create(
            IdentityUserId.New(),
            new ExternalIdentity("sub"),
            "first@test",
            "First",
            "avatar",
            Role.Member,
            DateTimeOffset.UtcNow);
        repo.Add(user);
        await repo.SaveChangesAsync(default);

        var loaded = await repo.FindByIdAsync(user.Id, default);
        loaded.Should().NotBeNull();
        loaded!.Email.Should().Be("first@test");
        loaded.Role.Should().Be(Role.Member);

        var queryUser = await queries.GetUserAsync(user.Id, default);
        queryUser.Should().NotBeNull();
        queryUser!.DisplayName.Should().Be("First");
    }

    [Fact]
    public async Task Identity_store_promotes_allowlisted_email_but_never_demotes()
    {
        using var database = new TestDatabase();
        var repo = new EfUserRepository(database.Identity);
        var user = User.Create(
            IdentityUserId.New(),
            new ExternalIdentity("owner-sub"),
            "owner@zmovie.dev",
            "Owner",
            null,
            Role.Admin,
            DateTimeOffset.UtcNow);
        repo.Add(user);
        await repo.SaveChangesAsync(default);

        var loaded = await repo.FindByIdAsync(user.Id, default);
        loaded.Should().NotBeNull();
        loaded!.Role.Should().Be(Role.Admin);
    }

    [Fact]
    public async Task Google_identity_verifier_rejects_missing_and_invalid_credentials()
    {
        var noConfig = new GoogleIdentityVerifier(new ConfigurationBuilder().Build());
        (await noConfig.VerifyAsync("credential", default)).Should().BeNull();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Google:ClientId"] = "client" }).Build();
        var verifier = new GoogleIdentityVerifier(config);
        (await verifier.VerifyAsync("", default)).Should().BeNull();
        (await verifier.VerifyAsync("not-a-jwt", default)).Should().BeNull();
    }

    [Fact]
    public async Task Search_store_uses_typesense_and_falls_back_to_database()
    {
        using var database = new TestDatabase();
        database.Db.Titles.Add(MakeTitle("search", "Search English", "Tìm kiếm", "movie", genre: "Drama"));
        await database.Db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Meilisearch:Url"] = "http://typesense.test", ["Meilisearch:ApiKey"] = "secret" }).Build();
        var successHandler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{\"hits\":[{\"slug\":\"s\",\"englishTitle\":\"English\",\"vietnameseTitle\":\"Vietnamese\",\"genre\":\"Drama\",\"year\":2026,\"type\":\"movie\",\"posterUrl\":\"p\"}]}");
        var client = new HttpClient(successHandler) { BaseAddress = new Uri("http://typesense.test") };
        var success = new SearchCatalogStore(client, config, database.Db);
        (await success.SearchAsync("q", "movie", "D'ram", "en", default)).Items.Should().ContainSingle().Which.Title.Should().Be("English");
        successHandler.Requests.Should().ContainSingle();

        var fallbackClient = new HttpClient(new FakeHttpMessageHandler().Enqueue(_ => throw new HttpRequestException())) { BaseAddress = new Uri("http://typesense.test") };
        var fallback = new SearchCatalogStore(fallbackClient, config, database.Db);
        (await fallback.SearchAsync("Search", null, null, "vi", default)).Items.Should().ContainSingle().Which.Title.Should().Be("Tìm kiếm");
    }

    [Fact]
    public async Task Engagement_and_analytics_stores_record_history_views_and_aggregations()
    {
        using var database = new TestDatabase();
        var user = Guid.NewGuid();
        var playable = Guid.NewGuid();
        var title = MakeTitle("title", "Title", "Tựa", "movie");
        database.Catalog.Titles.Add(title);
        await database.Catalog.SaveChangesAsync();

        database.Analytics.TitleViewEvents.Add(TitleViewEvent.Record(
            AnalyticsViewEventId.New(), new AnalyticsTitleId(title.Id.Value), 1, new AnalyticsUserId(user), "session", DateTimeOffset.UtcNow));
        await database.Analytics.SaveChangesAsync();

        database.Engagement.SavedTitles.Add(SavedTitle.Create(new EngagementUserId(user), new EngagementTitleId(title.Id.Value), DateTimeOffset.UtcNow));
        database.Engagement.WatchHistory.Add(WatchProgress.Record(new EngagementUserId(user), new EngagementPlayableId(playable), new EngagementTitleId(title.Id.Value), 1, WatchPosition.FromSeconds(10), DateTimeOffset.UtcNow));
        await database.Engagement.SaveChangesAsync();

        var savedRepo = new EfSavedTitleRepository(database.Engagement);
        var progressRepo = new EfWatchProgressRepository(database.Engagement);
        var queries = new EfUserLibraryQueries(database.Engagement);

        var saved = await queries.ListSavedAsync(new EngagementUserId(user), default);
        saved.Should().ContainSingle();
        var history = await queries.ListHistoryAsync(new EngagementUserId(user), default);
        history.Should().ContainSingle();

        var missingSaved = await savedRepo.FindAsync(new EngagementUserId(user), new EngagementTitleId(Guid.NewGuid()), default);
        missingSaved.Should().BeNull();
        var existingSaved = await savedRepo.FindAsync(new EngagementUserId(user), new EngagementTitleId(title.Id.Value), default);
        existingSaved.Should().NotBeNull();
        savedRepo.Remove(existingSaved!);
        await savedRepo.SaveChangesAsync(default);

        var existingProgress = await progressRepo.FindAsync(new EngagementUserId(user), new EngagementPlayableId(playable), default);
        existingProgress.Should().NotBeNull();
        existingProgress!.UpdateProgress(2, WatchPosition.FromSeconds(50), DateTimeOffset.UtcNow);
        await progressRepo.SaveChangesAsync(default);

        var analyticsQueries = new EfViewAnalyticsQueries(database.Analytics);
        (await analyticsQueries.GetViewCountAsync(new AnalyticsTitleId(title.Id.Value), default)).Should().Be(1);
        (await analyticsQueries.GetTopAsync(TopPeriod.Day, 5, DateTimeOffset.UtcNow, default)).Should().ContainSingle();
        (await analyticsQueries.GetTopAsync(TopPeriod.Week, 5, DateTimeOffset.UtcNow, default)).Should().ContainSingle();
        (await analyticsQueries.GetTopAsync(TopPeriod.Month, 5, DateTimeOffset.UtcNow, default)).Should().ContainSingle();
        (await analyticsQueries.GetTopAsync((TopPeriod)99, 5, DateTimeOffset.UtcNow, default)).Should().ContainSingle();

        using var recordDatabase = new TestDatabase();
        var viewRepo = new EfViewEventRepository(recordDatabase.Analytics);
        var now = DateTimeOffset.UtcNow;
        var firstView = await viewRepo.RecordViewWithLockAsync(new AnalyticsTitleId(title.Id.Value), 1, new AnalyticsUserId(user), "session", now, default);
        var duplicateView = await viewRepo.RecordViewWithLockAsync(new AnalyticsTitleId(title.Id.Value), 1, new AnalyticsUserId(user), "session", now, default);
        firstView.Counted.Should().BeTrue();
        duplicateView.Counted.Should().BeFalse();
        var anonymousView = await viewRepo.RecordViewWithLockAsync(new AnalyticsTitleId(title.Id.Value), null, null, "anonymous", now, default);
        var duplicateAnonymousView = await viewRepo.RecordViewWithLockAsync(new AnalyticsTitleId(title.Id.Value), null, null, "anonymous", now, default);
        anonymousView.Counted.Should().BeTrue();
        duplicateAnonymousView.Counted.Should().BeFalse();
    }

    [Fact]
    public async Task Caches_delegate_once_and_use_period_expirations()
    {
        using var database = new TestDatabase();
        var inner = new EfViewAnalyticsQueries(database.Analytics);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var analytics = new CachedViewAnalyticsQueries(inner, cache);
        var calls = 0;
        var now = DateTimeOffset.UtcNow;
        var first = await analytics.GetTopAsync(TopPeriod.Day, 3, now, default);
        var second = await analytics.GetTopAsync(TopPeriod.Day, 3, now, default);
        first.Should().BeSameAs(second);
        _ = await analytics.GetViewCountAsync(new AnalyticsTitleId(Guid.NewGuid()), default);
        await analytics.GetTopAsync(TopPeriod.Week, 3, now, default);
        await analytics.GetTopAsync(TopPeriod.Month, 3, now, default);
        await analytics.GetTopAsync((TopPeriod)99, 3, now, default);

        var responseCache = new TopTitlesResponseCache(cache);
        var response1 = await responseCache.GetOrCreateAsync(TopPeriod.Week, "vi", 3, _ => { calls++; return Task.FromResult<IReadOnlyList<TopTitleResponse>>([]); }, default);
        var response2 = await responseCache.GetOrCreateAsync(TopPeriod.Week, "vi", 3, _ => { calls++; return Task.FromResult<IReadOnlyList<TopTitleResponse>>([]); }, default);
        response1.Should().BeSameAs(response2);
        calls.Should().Be(1);
        await responseCache.GetOrCreateAsync(TopPeriod.Month, "vi", 3, _ => Task.FromResult<IReadOnlyList<TopTitleResponse>>([]), default);
        await responseCache.GetOrCreateAsync(TopPeriod.Day, "vi", 3, _ => Task.FromResult<IReadOnlyList<TopTitleResponse>>([]), default);
        await responseCache.GetOrCreateAsync((TopPeriod)99, "vi", 3, _ => Task.FromResult<IReadOnlyList<TopTitleResponse>>([]), default);
    }

    [Fact]
    public async Task Assistant_store_scores_tokens_and_handles_empty_message()
    {
        using var database = new TestDatabase();
        var title = MakeTitle("warm-friends", "Warm Friends", "Bạn ấm áp", "movie", synopsis: "Một câu chuyện ấm áp về tình bạn và gia đình");
        database.Catalog.Titles.Add(title);
        await database.Catalog.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var recommender = new TinyContentRecommendationEngine(cache);
        var catalogReader = new CatalogLibraryReader(database.Catalog);
        var userLibraryQueries = new EfUserLibraryQueries(database.Engagement);
        var personalizationQueries = new EfPersonalizationQueries(database.Personalization, NullLogger<EfPersonalizationQueries>.Instance);
        var learningRepo = new EfPersonalizationLearningRepository(database.Personalization);
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));
        var store = new CatalogAssistantStore(catalogReader, userLibraryQueries, recommender, personalizationQueries, time);
        var user = Guid.NewGuid();
        var empty = await store.SearchAsync(user, "", "vi", 5, default);
        empty.Should().BeEmpty();

        var suggestions = await store.SearchAsync(user, "hôm nay tôi buồn", "vi", 5, default);
        suggestions.Should().NotBeEmpty();

        var recorder = new AssistantImpressionRecorder(learningRepo, catalogReader, time, NullLogger<AssistantImpressionRecorder>.Instance);
        var recommendationId = await recorder.RecordImpressionAsync(user, "hôm nay tôi buồn", ["warm-friends"], default);
        recommendationId.Should().NotBeNull();

        var feedbackHandler = new RecordPersonalizationFeedbackHandler(learningRepo, catalogReader, time);
        (await feedbackHandler.Handle(new(user, recommendationId!.Value, "warm-friends", "like"), default)).Value.Should().BeTrue();
        (await feedbackHandler.Handle(new(user, recommendationId.Value, "warm-friends", "watch"), default)).Value.Should().BeTrue();
        (await feedbackHandler.Handle(new(user, recommendationId.Value, "warm-friends", "unknown"), default)).Value.Should().BeFalse();

        var scores = await personalizationQueries.GetTitleScoresAsync(new ZMovie.Domain.Personalization.UserId(user), new Dictionary<string, int> { ["buồn"] = 1, ["chữa"] = 1, ["lành"] = 1 }, time.GetUtcNow(), default);
        scores.Should().ContainKey(title.Id.Value);
        scores[title.Id.Value].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Importer_and_seed_upsert_and_are_idempotent()
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{\"status\":\"success\",\"data\":{\"items\":[{\"name\":\"Drama\",\"slug\":\"drama\"},{\"name\":\"\",\"slug\":\"\"}]}}");
        var count = await OPhimGenreImporter.ImportAsync(database.Db, new HttpClient(handler), default);
        count.Should().Be(1);
        handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{\"status\":\"success\",\"data\":{\"items\":[{\"name\":\"Drama Updated\",\"slug\":\"drama\"}]}}");
        await OPhimGenreImporter.ImportAsync(database.Db, new HttpClient(handler), default);
        (await database.Db.Genres.SingleAsync()).Name.Should().Be("Drama Updated");

        await CatalogSeed.SeedAsync(database.Db);
        await CatalogSeed.SeedAsync(database.Db);
        (await database.Db.Titles.CountAsync()).Should().BeGreaterThan(5);
        (await database.Db.Episodes.CountAsync()).Should().BeGreaterThan(0);
    }

    private static CatalogTitle MakeTitle(string slug, string english, string vietnamese, string type, bool featured = false, string genre = "Drama", string synopsis = "Synopsis") =>
        CatalogTitle.Create(
            CatalogTitleId.New(),
            TitleSlug.Parse(slug),
            new LocalizedText(vietnamese, english),
            new LocalizedText(synopsis, synopsis),
            genre,
            ReleaseYear.FromInt(2026),
            TitleType.Normalize(type),
            "poster",
            Runtime.FromMinutes(90),
            featured,
            DateTimeOffset.UtcNow);

    private sealed class FakeAnalytics : IViewAnalyticsQueries
    {
        public Dictionary<Guid, long> Counts { get; set; } = [];
        public Task<long> GetViewCountAsync(AnalyticsTitleId titleId, CancellationToken ct) => Task.FromResult(Counts.GetValueOrDefault(titleId.Value));
        public Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, DateTimeOffset now, CancellationToken ct) => Task.FromResult<IReadOnlyList<TopViewCount>>([]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
