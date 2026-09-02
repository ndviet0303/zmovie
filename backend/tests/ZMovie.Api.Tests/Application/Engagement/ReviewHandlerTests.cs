using ErrorOr;
using FluentAssertions;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using Xunit;

namespace ZMovie.Api.Tests.Application.Engagement;

public sealed class ReviewHandlerTests
{
    private static readonly Guid RawTitleId = Guid.Parse("5f5742aa-77d7-465b-8377-cb64d6110d2a");
    private static readonly Guid RawUserId = Guid.Parse("c480bbcf-5c8e-4c75-9fe5-99dc035a141a");
    private static readonly ReviewId ReviewId = new(Guid.Parse("01993e3f-49db-7c77-bdc5-502ef26f28c3"));

    [Fact]
    public async Task Submit_creates_review_through_repository_with_fixed_time()
    {
        var occurredAt = new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero);
        var repository = new FakeReviewRepository();
        var handler = new SubmitTitleReviewHandler(
            repository,
            new FakeCatalogReader(RawTitleId),
            new FixedTimeProvider(occurredAt));

        var result = await handler.Handle(
            new SubmitTitleReviewCommand(RawUserId, "Lan", "first", 9, "  Worth watching  "),
            default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.RequestedTitleId.Should().Be(new TitleId(RawTitleId));
        repository.RequestedUserId.Should().Be(new UserId(RawUserId));
        repository.AddedReview.Should().NotBeNull();
        repository.AddedReview!.TitleId.Should().Be(new TitleId(RawTitleId));
        repository.AddedReview.UserId.Should().Be(new UserId(RawUserId));
        repository.AddedReview.AuthorName.Should().Be("Lan");
        repository.AddedReview.Rating.Value.Should().Be(9);
        repository.AddedReview.Comment.Should().Be("Worth watching");
        repository.AddedReview.CreatedAt.Should().Be(occurredAt);
        repository.AddedReview.UpdatedAt.Should().Be(occurredAt);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Submit_edits_existing_review_through_repository_with_fixed_time()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 9, 30, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(1);
        var existing = CreateReview(createdAt);
        var repository = new FakeReviewRepository(existing);
        var handler = new SubmitTitleReviewHandler(
            repository,
            new FakeCatalogReader(RawTitleId),
            new FixedTimeProvider(updatedAt));

        var result = await handler.Handle(
            new SubmitTitleReviewCommand(RawUserId, "Minh", "first", 10, "  Updated  "),
            default);

        result.IsError.Should().BeFalse();
        repository.AddedReview.Should().BeNull();
        existing.AuthorName.Should().Be("Minh");
        existing.Rating.Value.Should().Be(10);
        existing.Comment.Should().Be("Updated");
        existing.CreatedAt.Should().Be(createdAt);
        existing.UpdatedAt.Should().Be(updatedAt);
        repository.SaveChangesCalls.Should().Be(1);
    }

    public static TheoryData<string, int, string?, string> RejectedReviewData => new()
    {
        { "Lan", Rating.Minimum - 1, null, nameof(SubmitTitleReviewCommand.Rating) },
        { "   ", 8, null, nameof(SubmitTitleReviewCommand.AuthorName) },
        { new string('a', Review.MaximumAuthorNameLength + 1), 8, null, nameof(SubmitTitleReviewCommand.AuthorName) },
        { "Lan", 8, new string('c', Review.MaximumCommentLength + 1), nameof(SubmitTitleReviewCommand.Comment) },
    };

    [Theory]
    [MemberData(nameof(RejectedReviewData))]
    public async Task Submit_maps_domain_rejection_without_adding_or_saving(
        string authorName,
        int rating,
        string? comment,
        string expectedCode)
    {
        var repository = new FakeReviewRepository();
        var handler = new SubmitTitleReviewHandler(
            repository,
            new FakeCatalogReader(RawTitleId),
            new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        var result = await handler.Handle(
            new SubmitTitleReviewCommand(RawUserId, authorName, "first", rating, comment),
            default);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be(expectedCode);
        repository.AddedReview.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Remove_deletes_matching_review_and_saves()
    {
        var existing = CreateReview(DateTimeOffset.UnixEpoch);
        var repository = new FakeReviewRepository(existing);
        var handler = new RemoveTitleReviewHandler(repository, new FakeCatalogReader(RawTitleId));

        var result = await handler.Handle(new RemoveTitleReviewCommand(RawUserId, "first"), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.RemovedReview.Should().BeSameAs(existing);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Remove_preserves_review_not_found_for_missing_title_or_review(bool titleExists)
    {
        var repository = new FakeReviewRepository();
        var handler = new RemoveTitleReviewHandler(
            repository,
            new FakeCatalogReader(titleExists ? RawTitleId : null));

        var result = await handler.Handle(new RemoveTitleReviewCommand(RawUserId, "missing"), default);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("engagement.review.not_found");
        repository.RemovedReview.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Admin_delete_removes_review_through_engagement_repository_and_saves()
    {
        var existing = CreateReview(DateTimeOffset.UnixEpoch);
        var repository = new FakeReviewRepository(existing);
        var handler = new DeleteReviewHandler(repository);

        var result = await handler.Handle(new DeleteReviewCommand(ReviewId.Value), default);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        repository.RequestedReviewId.Should().Be(ReviewId);
        repository.RemovedReview.Should().BeSameAs(existing);
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Admin_delete_preserves_admin_review_not_found_without_saving()
    {
        var repository = new FakeReviewRepository();
        var handler = new DeleteReviewHandler(repository);

        var result = await handler.Handle(new DeleteReviewCommand(ReviewId.Value), default);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("admin.review.not_found");
        repository.RequestedReviewId.Should().Be(ReviewId);
        repository.RemovedReview.Should().BeNull();
        repository.SaveChangesCalls.Should().Be(0);
    }

    private static Review CreateReview(DateTimeOffset occurredAt) =>
        Review.Create(
            ReviewId,
            new TitleId(RawTitleId),
            new UserId(RawUserId),
            "Lan",
            8,
            "Original",
            occurredAt).Review!;

    private sealed class FakeReviewRepository(Review? review = null) : IReviewRepository
    {
        private Review? _review = review;

        public TitleId? RequestedTitleId { get; private set; }
        public UserId? RequestedUserId { get; private set; }
        public ReviewId? RequestedReviewId { get; private set; }
        public Review? AddedReview { get; private set; }
        public Review? RemovedReview { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task<Review?> FindByTitleAndUserAsync(TitleId titleId, UserId userId, CancellationToken ct)
        {
            RequestedTitleId = titleId;
            RequestedUserId = userId;
            return Task.FromResult(
                _review is not null && _review.TitleId == titleId && _review.UserId == userId
                    ? _review
                    : null);
        }

        public Task<Review?> FindByIdAsync(ReviewId reviewId, CancellationToken ct)
        {
            RequestedReviewId = reviewId;
            return Task.FromResult(_review?.Id == reviewId ? _review : null);
        }

        public void Add(Review review)
        {
            AddedReview = review;
            _review = review;
        }

        public void Remove(Review review)
        {
            RemovedReview = review;
            if (ReferenceEquals(_review, review)) _review = null;
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCatalogReader(Guid? titleId) : ILibraryCatalogReader
    {
        public Task<Guid?> FindTitleIdAsync(string slug, CancellationToken ct) => Task.FromResult(titleId);

        public Task<PlayableReference?> FindPlayableAsync(string slug, int? episodeNumber, CancellationToken ct) =>
            Task.FromResult<PlayableReference?>(null);

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
