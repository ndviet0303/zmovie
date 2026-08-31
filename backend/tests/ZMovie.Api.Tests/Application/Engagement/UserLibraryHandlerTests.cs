using ErrorOr;
using FluentAssertions;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using Xunit;

namespace ZMovie.Api.Tests.Application.Engagement;

public sealed class UserLibraryHandlerTests
{
    private static readonly Guid RawUserId = Guid.Parse("c480bbcf-5c8e-4c75-9fe5-99dc035a141a");
    private static readonly Guid RawTitleId = Guid.Parse("5f5742aa-77d7-465b-8377-cb64d6110d2a");
    private static readonly Guid RawPlayableId = Guid.Parse("01993e3f-49db-7c77-bdc5-502ef26f28c3");

    [Fact]
    public async Task Save_creates_saved_title_through_repository_with_fixed_time()
    {
        var savedAt = new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero);
        var repository = new FakeSavedTitleRepository();
        var handler = new SaveTitleHandler(
            repository,
            new FakeCatalogReader(RawTitleId),
            new FixedTimeProvider(savedAt));

        var result = await handler.Handle(new SaveTitleCommand(RawUserId, "slug-1"), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.AddedSavedTitle.Should().NotBeNull();
        repository.AddedSavedTitle!.UserId.Should().Be(new UserId(RawUserId));
        repository.AddedSavedTitle.TitleId.Should().Be(new TitleId(RawTitleId));
        repository.AddedSavedTitle.SavedAt.Should().Be(savedAt);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Save_is_idempotent_when_title_already_saved()
    {
        var existing = SavedTitle.Create(new UserId(RawUserId), new TitleId(RawTitleId), DateTimeOffset.UnixEpoch);
        var repository = new FakeSavedTitleRepository(existing);
        var handler = new SaveTitleHandler(
            repository,
            new FakeCatalogReader(RawTitleId),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(new SaveTitleCommand(RawUserId, "slug-1"), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.AddedSavedTitle.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Save_returns_catalog_not_found_when_title_does_not_exist()
    {
        var repository = new FakeSavedTitleRepository();
        var handler = new SaveTitleHandler(
            repository,
            new FakeCatalogReader(null),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(new SaveTitleCommand(RawUserId, "missing"), default);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("catalog.title.not_found");
        repository.AddedSavedTitle.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Remove_deletes_matching_saved_title_and_saves()
    {
        var existing = SavedTitle.Create(new UserId(RawUserId), new TitleId(RawTitleId), DateTimeOffset.UnixEpoch);
        var repository = new FakeSavedTitleRepository(existing);
        var handler = new RemoveSavedTitleHandler(repository, new FakeCatalogReader(RawTitleId));

        var result = await handler.Handle(new RemoveSavedTitleCommand(RawUserId, "slug-1"), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.RemovedSavedTitle.Should().BeSameAs(existing);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Remove_returns_saved_not_found_for_missing_title_or_missing_save(bool titleExists)
    {
        var repository = new FakeSavedTitleRepository();
        var handler = new RemoveSavedTitleHandler(
            repository,
            new FakeCatalogReader(titleExists ? RawTitleId : null));

        var result = await handler.Handle(new RemoveSavedTitleCommand(RawUserId, "missing"), default);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("engagement.saved.not_found");
        repository.RemovedSavedTitle.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Record_progress_creates_new_progress_with_fixed_time()
    {
        var updatedAt = new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);
        var repository = new FakeWatchProgressRepository();
        var playable = new PlayableReference(RawTitleId, RawPlayableId, 1);
        var handler = new RecordWatchProgressHandler(
            repository,
            new FakeCatalogReader(RawTitleId, playable),
            new FixedTimeProvider(updatedAt));

        var result = await handler.Handle(
            new RecordWatchProgressCommand(RawUserId, "slug-1", 1, 120.5),
            default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.AddedProgress.Should().NotBeNull();
        repository.AddedProgress!.UserId.Should().Be(new UserId(RawUserId));
        repository.AddedProgress.PlayableId.Should().Be(new PlayableId(RawPlayableId));
        repository.AddedProgress.TitleId.Should().Be(new TitleId(RawTitleId));
        repository.AddedProgress.EpisodeNumber.Should().Be(1);
        repository.AddedProgress.Position.Seconds.Should().Be(120.5);
        repository.AddedProgress.UpdatedAt.Should().Be(updatedAt);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Record_progress_updates_existing_progress_with_fixed_time()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(1);
        var existing = WatchProgress.Record(
            new UserId(RawUserId),
            new PlayableId(RawPlayableId),
            new TitleId(RawTitleId),
            1,
            WatchPosition.FromSeconds(10),
            createdAt);
        var repository = new FakeWatchProgressRepository(existing);
        var playable = new PlayableReference(RawTitleId, RawPlayableId, 1);
        var handler = new RecordWatchProgressHandler(
            repository,
            new FakeCatalogReader(RawTitleId, playable),
            new FixedTimeProvider(updatedAt));

        var result = await handler.Handle(
            new RecordWatchProgressCommand(RawUserId, "slug-1", 1, 300),
            default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.AddedProgress.Should().BeNull();
        existing.Position.Seconds.Should().Be(300);
        existing.UpdatedAt.Should().Be(updatedAt);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Record_progress_returns_playable_not_found_when_playable_does_not_exist()
    {
        var repository = new FakeWatchProgressRepository();
        var handler = new RecordWatchProgressHandler(
            repository,
            new FakeCatalogReader(RawTitleId, null),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(
            new RecordWatchProgressCommand(RawUserId, "slug-1", 99, 10),
            default);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("catalog.playable.not_found");
        repository.AddedProgress.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    private sealed class FakeSavedTitleRepository(SavedTitle? savedTitle = null) : ISavedTitleRepository
    {
        private SavedTitle? _saved = savedTitle;

        public SavedTitle? AddedSavedTitle { get; private set; }
        public SavedTitle? RemovedSavedTitle { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task<SavedTitle?> FindAsync(UserId userId, TitleId titleId, CancellationToken ct) =>
            Task.FromResult(_saved is not null && _saved.UserId == userId && _saved.TitleId == titleId ? _saved : null);

        public void Add(SavedTitle savedTitle)
        {
            AddedSavedTitle = savedTitle;
            _saved = savedTitle;
        }

        public void Remove(SavedTitle savedTitle)
        {
            RemovedSavedTitle = savedTitle;
            if (ReferenceEquals(_saved, savedTitle)) _saved = null;
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWatchProgressRepository(WatchProgress? progress = null) : IWatchProgressRepository
    {
        private WatchProgress? _progress = progress;

        public WatchProgress? AddedProgress { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task<WatchProgress?> FindAsync(UserId userId, PlayableId playableId, CancellationToken ct) =>
            Task.FromResult(_progress is not null && _progress.UserId == userId && _progress.PlayableId == playableId ? _progress : null);

        public void Add(WatchProgress progress)
        {
            AddedProgress = progress;
            _progress = progress;
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCatalogReader(Guid? titleId, PlayableReference? playable = null) : ILibraryCatalogReader
    {
        public Task<Guid?> FindTitleIdAsync(string slug, CancellationToken ct) => Task.FromResult(titleId);

        public Task<PlayableReference?> FindPlayableAsync(string slug, int? episodeNumber, CancellationToken ct) =>
            Task.FromResult(playable);

        public Task<IReadOnlyDictionary<Guid, LibraryTitle>> GetTitlesAsync(
            IEnumerable<Guid> titleIds,
            string locale,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, LibraryTitle>>(new Dictionary<Guid, LibraryTitle>());

        public Task<IReadOnlyList<LibraryTitle>> GetDiscoveryTitlesAsync(string locale, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<LibraryTitle>>([]);

        public Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationCandidatesAsync(
            string locale,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RecommendationCandidate>>([]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
