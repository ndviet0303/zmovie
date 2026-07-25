using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZMovie.Application.Assistant;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Assistant;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Identity;
using ZMovie.Infrastructure.Persistence;
using ZMovie.Infrastructure.Recommendations;
using ZMovie.Infrastructure.Recommendations.Models;
using ZMovie.Infrastructure.Search;
using ZMovie.Infrastructure.Seed;
using Xunit;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task Catalog_readers_and_stores_cover_empty_and_populated_paths()
    {
        using var database = new TestDatabase();
        var title = Title("first", "First", "Đầu tiên", "movie", featured: true);
        var series = Title("series", "Series", "Bộ phim", "series", featured: false);
        database.Db.Titles.AddRange(title, series);
        database.Db.Episodes.Add(new CatalogEpisode { TitleId = series.Id, Number = 1, Name = "Episode 1", HlsUrl = "https://video/1" });
        await database.Db.SaveChangesAsync();

        var reader = new CatalogLibraryReader(database.Db);
        (await reader.FindTitleIdAsync("first", default)).Should().Be(title.Id);
        (await reader.FindTitleIdAsync("missing", default)).Should().BeNull();
        (await reader.FindPlayableAsync("missing", null, default)).Should().BeNull();
        (await reader.FindPlayableAsync("first", null, default)).Should().NotBeNull();
        (await reader.FindPlayableAsync("series", 1, default)).Should().NotBeNull();
        (await reader.FindPlayableAsync("series", 9, default)).Should().BeNull();
        (await reader.GetTitlesAsync([], "vi", default)).Should().BeEmpty();
        (await reader.GetTitlesAsync([title.Id], "en", default)).Should().ContainKey(title.Id);
        (await reader.GetDiscoveryTitlesAsync("vi", default)).Should().HaveCount(2);
        (await reader.GetRecommendationCandidatesAsync("en", default)).Should().HaveCount(2);

        var analytics = new FakeAnalytics { Counts = new Dictionary<Guid, long> { [title.Id] = 7 } };
        var store = new EfCatalogReadStore(database.Db, analytics);
        (await store.ListAsync("First", null, "en", default)).Items.Should().ContainSingle();
        var genreQuery = () => store.ListAsync(null, "Drama", "vi", default);
        await genreQuery.Should().ThrowAsync<InvalidOperationException>();
        (await store.ListAsync(null, null, "vi", default)).Should().BeOfType<TitleListResponse>();
        var detail = await store.GetAsync("first", "en", default);
        detail.Should().NotBeNull();
        detail!.ViewCount.Should().Be(7);
        (await store.GetAsync("missing", "vi", default)).Should().BeNull();
        var playback = await store.GetPlaybackAsync("series", "vi", default);
        playback.Should().NotBeNull();
        playback!.IsSeries.Should().BeTrue();
        (await store.GetPlaybackAsync("missing", "vi", default)).Should().BeNull();
        (await store.GetHomeAsync("vi", default)).Should().NotBeNull();

        database.Db.Genres.Add(new CatalogGenre { Slug = "drama", Name = "Drama" });
        await database.Db.SaveChangesAsync();
        var importedGenres = await store.GetGenresAsync(default);
        importedGenres.Should().ContainSingle();
        importedGenres[0].Should().Be("Drama");
        database.Db.Genres.RemoveRange(database.Db.Genres);
        await database.Db.SaveChangesAsync();
        (await store.GetGenresAsync(default)).Should().Contain("Drama");
    }

    [Fact]
    public async Task Catalog_home_uses_featured_fallback_and_returns_null_when_empty()
    {
        using var empty = new TestDatabase();
        var emptyStore = new EfCatalogReadStore(empty.Db, new FakeAnalytics());
        (await emptyStore.GetHomeAsync("vi", default)).Should().BeNull();

        using var database = new TestDatabase();
        database.Db.Titles.Add(Title("featured", "Featured", "Nổi bật", "movie", featured: true));
        await database.Db.SaveChangesAsync();
        var home = await new EfCatalogReadStore(database.Db, new FakeAnalytics()).GetHomeAsync("en", default);
        home.Should().NotBeNull();
        home!.Hero.Slug.Should().Be("featured");
    }

    [Fact]
    public async Task User_library_store_handles_saved_progress_reviews_and_rankings()
    {
        using var database = new TestDatabase();
        var user = Guid.NewGuid();
        var title = Title("title", "Title", "Tiêu đề", "series");
        var playable = Guid.NewGuid();
        database.Db.Titles.Add(title);
        database.Db.SavedTitles.Add(new SavedTitle { UserId = user, TitleId = title.Id });
        database.Db.WatchHistory.Add(new WatchProgress { UserId = user, TitleId = title.Id, PlayableId = playable, EpisodeNumber = 1, ProgressSeconds = 10 });
        database.Db.TitleViewEvents.Add(new TitleViewEvent { TitleId = title.Id, SessionId = "s" });
        await database.Db.SaveChangesAsync();

        var store = new EfUserLibraryStore(database.Db);
        (await store.GetSavedAsync(user, default)).Should().ContainSingle();
        (await store.GetHistoryAsync(user, default)).Should().ContainSingle();
        await store.SaveAsync(user, title.Id, default);
        await store.SaveAsync(user, Guid.NewGuid(), default);
        (await store.RemoveAsync(user, Guid.NewGuid(), default)).Should().BeFalse();
        (await store.RemoveAsync(user, title.Id, default)).Should().BeTrue();
        await store.RecordProgressAsync(user, new PlayableReference(title.Id, playable, 2), -5, default);
        await store.RecordProgressAsync(user, new PlayableReference(title.Id, playable, 3), 50, default);
        (await store.GetViewCountAsync(title.Id, default)).Should().Be(1);
        (await store.GetTopAsync(TopPeriod.Day, 5, default)).Should().ContainSingle();
        (await store.GetTopAsync(TopPeriod.Week, 5, default)).Should().ContainSingle();
        (await store.GetTopAsync(TopPeriod.Month, 5, default)).Should().ContainSingle();
        (await store.GetTopAsync((TopPeriod)99, 5, default)).Should().ContainSingle();

        await store.UpsertAsync(title.Id, user, "User", 8, "comment", default);
        await store.UpsertAsync(title.Id, user, "Updated", 9, "  ", default);
        (await store.GetAsync(title.Id, default)).Should().ContainSingle().Which.Comment.Should().BeNull();
        (await store.RemoveReviewAsync(title.Id, Guid.NewGuid(), default)).Should().BeFalse();
        (await store.RemoveReviewAsync(title.Id, user, default)).Should().BeTrue();

        using var recordDatabase = new TestDatabase();
        var recordStore = new EfUserLibraryStore(recordDatabase.Db);
        var firstView = await recordStore.RecordAsync(title.Id, user, "session", 1, default);
        var duplicateView = await recordStore.RecordAsync(title.Id, user, "session", 1, default);
        firstView.Counted.Should().BeTrue();
        duplicateView.Counted.Should().BeFalse();
        var anonymousView = await recordStore.RecordAsync(title.Id, null, "anonymous", null, default);
        var duplicateAnonymousView = await recordStore.RecordAsync(title.Id, null, "anonymous", null, default);
        anonymousView.Counted.Should().BeTrue();
        duplicateAnonymousView.Counted.Should().BeFalse();
    }

    [Fact]
    public async Task Caches_delegate_once_and_use_period_expirations()
    {
        using var database = new TestDatabase();
        var inner = new EfUserLibraryStore(database.Db);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var analytics = new CachedViewAnalyticsStore(inner, cache);
        var calls = 0;
        var first = await analytics.GetTopAsync(TopPeriod.Day, 3, default);
        var second = await analytics.GetTopAsync(TopPeriod.Day, 3, default);
        first.Should().BeSameAs(second);
        _ = await analytics.GetViewCountAsync(Guid.NewGuid(), default);
        await analytics.GetTopAsync(TopPeriod.Week, 3, default);
        await analytics.GetTopAsync(TopPeriod.Month, 3, default);
        await analytics.GetTopAsync((TopPeriod)99, 3, default);
        _ = await analytics.RecordAsync(Guid.NewGuid(), null, "session", null, default);
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
        database.Db.Titles.AddRange(
            Title("dragon", "Dragon Quest", "Nhiệm vụ rồng", "movie", genre: "Adventure", synopsis: "A brave dragon adventure"),
            Title("warm-friends", "Warm Friends", "Những người bạn ấm áp", "movie", genre: "Family", synopsis: "Một câu chuyện chữa lành nhẹ nhàng về hy vọng và tình bạn"));
        await database.Db.SaveChangesAsync();
        var store = new CatalogAssistantStore(database.Db);
        (await store.SearchAsync(Guid.NewGuid(), "!!!", "vi", 3, default)).Should().BeEmpty();
        (await store.SearchAsync(Guid.NewGuid(), "dragon adventure", "en", 3, default)).Should().ContainSingle().Which.Title.Title.Should().Be("Dragon Quest");
        (await store.SearchAsync(Guid.NewGuid(), "hôm nay tôi buồn", "vi", 3, default)).Should().ContainSingle().Which.Title.Slug.Should().Be("warm-friends");
    }

    [Fact]
    public async Task Personalized_assistant_retriever_uses_history_and_saved_titles()
    {
        using var database = new TestDatabase();
        var user = Guid.NewGuid();
        var watched = Title("watched", "Space Journey", "Hành trình không gian", "movie", genre: "Sci Fi", synopsis: "A space adventure");
        var recommended = Title("recommended", "Deep Space", "Không gian sâu", "movie", genre: "Sci Fi", synopsis: "Another space adventure");
        var unrelated = Title("unrelated", "Quiet Garden", "Khu vườn yên tĩnh", "movie", genre: "Drama", synopsis: "A quiet garden");
        database.Db.Titles.AddRange(watched, recommended, unrelated);
        database.Db.SavedTitles.Add(new ZMovie.Domain.Engagement.SavedTitle { UserId = user, TitleId = watched.Id });
        await database.Db.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var store = new CatalogAssistantStore(database.Db, new EfUserLibraryStore(database.Db), new CatalogLibraryReader(database.Db), new TinyContentRecommendationEngine(cache));
        var results = await store.SearchAsync(user, "weekend", "en", 1, default);

        results.Should().ContainSingle().Which.Title.Slug.Should().Be("recommended");
    }

    [Fact]
    public async Task Local_ai_generator_returns_reply_and_falls_back_for_http_errors()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{\"reply\":\"Try Deep Space.\"}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://ollama.test/") };
        var generator = new LocalAiAssistantTextGenerator(http, Options.Create(new LocalAiOptions { Enabled = true }), NullLogger<LocalAiAssistantTextGenerator>.Instance);
        var request = new AssistantGenerationRequest("space", "en", [new(new("deep-space", "Deep Space", "Sci Fi", 2026, "movie", "poster"), "A space adventure")]);
        (await generator.GenerateAsync(request, default)).Should().Be("Try Deep Space.");
        handler.Requests.Should().ContainSingle().Which.AbsolutePath.Should().Be("/v1/chat");

        using var unavailableHttp = new HttpClient(new FakeHttpMessageHandler().Enqueue(_ => throw new HttpRequestException())) { BaseAddress = new Uri("http://ollama.test/") };
        var unavailable = new LocalAiAssistantTextGenerator(unavailableHttp, Options.Create(new LocalAiOptions { Enabled = true }), NullLogger<LocalAiAssistantTextGenerator>.Instance);
        (await unavailable.GenerateAsync(request, default)).Should().BeNull();
        using var timeoutHttp = new HttpClient(new FakeHttpMessageHandler().Enqueue(_ => throw new OperationCanceledException())) { BaseAddress = new Uri("http://ollama.test/") };
        var timeout = new LocalAiAssistantTextGenerator(timeoutHttp, Options.Create(new LocalAiOptions { Enabled = true }), NullLogger<LocalAiAssistantTextGenerator>.Instance);
        (await timeout.GenerateAsync(request, default)).Should().BeNull();
        var invalidConfig = new LocalAiAssistantTextGenerator(new HttpClient(), Options.Create(new LocalAiOptions { Enabled = true }), NullLogger<LocalAiAssistantTextGenerator>.Instance);
        (await invalidConfig.GenerateAsync(request, default)).Should().BeNull();
        var disabled = new LocalAiAssistantTextGenerator(new HttpClient(), Options.Create(new LocalAiOptions()), NullLogger<LocalAiAssistantTextGenerator>.Instance);
        (await disabled.GenerateAsync(request, default)).Should().BeNull();
    }

    [Fact]
    public void Recommendation_model_and_engine_rank_candidates_and_cache_model()
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
        var store = new EfUserIdentityStore(database.Db);
        var first = await store.UpsertGoogleUserAsync(new("sub", "first@test", "First", "avatar"), default);
        var second = await store.UpsertGoogleUserAsync(new("sub", "second@test", "Second", null), default);
        second.Id.Should().Be(first.Id);
        second.Email.Should().Be("second@test");
        second.AvatarUrl.Should().BeNull();
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
    public async Task Search_store_uses_meilisearch_and_falls_back_to_database()
    {
        using var database = new TestDatabase();
        database.Db.Titles.Add(Title("search", "Search English", "Tìm kiếm", "movie", genre: "Drama"));
        await database.Db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Meilisearch:Url"] = "http://search.test", ["Meilisearch:ApiKey"] = "secret" }).Build();
        var successHandler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{\"hits\":[{\"slug\":\"s\",\"englishTitle\":\"English\",\"vietnameseTitle\":\"Vietnamese\",\"genre\":\"Drama\",\"year\":2026,\"type\":\"movie\",\"posterUrl\":\"p\"}]}");
        var success = new SearchCatalogStore(new HttpClient(successHandler), config, database.Db);
        (await success.SearchAsync("q", "movie", "D'ram", "en", default)).Items.Should().ContainSingle().Which.Title.Should().Be("English");
        successHandler.Requests.Should().ContainSingle();

        var fallback = new SearchCatalogStore(new HttpClient(new FakeHttpMessageHandler().Enqueue(_ => throw new HttpRequestException())), config, database.Db);
        (await fallback.SearchAsync("Search", null, null, "vi", default)).Items.Should().ContainSingle().Which.Title.Should().Be("Tìm kiếm");
    }

    [Fact]
    public async Task Genre_importer_and_catalog_seed_handle_new_and_existing_rows()
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

    private static CatalogTitle Title(string slug, string english, string vietnamese, string type, bool featured = false, string genre = "Drama", string synopsis = "Synopsis") => new()
    {
        Slug = slug,
        EnglishTitle = english,
        VietnameseTitle = vietnamese,
        EnglishSynopsis = synopsis,
        VietnameseSynopsis = synopsis,
        Genre = genre,
        Year = 2026,
        Type = type,
        PosterUrl = "poster",
        RuntimeMinutes = 90,
        Featured = featured
    };

    private sealed class FakeAnalytics : IViewAnalyticsStore
    {
        public Dictionary<Guid, long> Counts { get; set; } = [];
        public Task<ViewRecordedResponse> RecordAsync(Guid titleId, Guid? userId, string sessionId, int? episodeNumber, CancellationToken ct) => Task.FromResult(new ViewRecordedResponse(0, true));
        public Task<long> GetViewCountAsync(Guid titleId, CancellationToken ct) => Task.FromResult(Counts.GetValueOrDefault(titleId));
        public Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, CancellationToken ct) => Task.FromResult<IReadOnlyList<TopViewCount>>([]);
    }
}
