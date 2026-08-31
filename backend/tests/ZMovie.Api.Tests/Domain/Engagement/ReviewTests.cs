using System.Reflection;
using ZMovie.Domain.Engagement;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Engagement;

public sealed class ReviewTests
{
    private static readonly ReviewId Id = new(Guid.Parse("01993e3f-49db-7c77-bdc5-502ef26f28c3"));
    private static readonly TitleId TitleId = new(Guid.Parse("dd1fd52c-0fac-48bc-bf7c-eb48a7146791"));
    private static readonly UserId UserId = new(Guid.Parse("67d06bf3-913b-4a55-b6ac-e758ecf9d0d7"));

    [Theory]
    [InlineData(Rating.Minimum)]
    [InlineData(Rating.Maximum)]
    public void Rating_accepts_inclusive_boundaries(int value)
    {
        var accepted = Rating.TryCreate(value, out var rating);

        Assert.True(accepted);
        Assert.Equal(value, rating.Value);
    }

    [Theory]
    [InlineData(Rating.Minimum - 1)]
    [InlineData(Rating.Maximum + 1)]
    public void Rating_rejects_values_outside_range(int value)
    {
        var accepted = Rating.TryCreate(value, out var rating);

        Assert.False(accepted);
        Assert.Equal(default, rating);
    }

    [Fact]
    public void Create_sets_identity_and_same_explicit_timestamps()
    {
        var occurredAt = new DateTimeOffset(2026, 8, 31, 8, 15, 0, TimeSpan.Zero);

        var decision = Review.Create(Id, TitleId, UserId, "Lan", 8, "  Worth watching  ", occurredAt);

        Assert.True(decision.IsAccepted);
        var review = Assert.IsType<Review>(decision.Review);
        Assert.Equal(Id, review.Id);
        Assert.Equal(TitleId, review.TitleId);
        Assert.Equal(UserId, review.UserId);
        Assert.Equal("Lan", review.AuthorName);
        Assert.Equal(8, review.Rating.Value);
        Assert.Equal("Worth watching", review.Comment);
        Assert.Equal(occurredAt, review.CreatedAt);
        Assert.Equal(occurredAt, review.UpdatedAt);
        Assert.Null(decision.Rejection);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void Create_normalizes_blank_comment_to_null(string? comment)
    {
        var decision = Review.Create(Id, TitleId, UserId, "Lan", 8, comment, DateTimeOffset.UnixEpoch);

        Assert.True(decision.IsAccepted);
        Assert.Null(decision.Review!.Comment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_rejects_invalid_rating_without_creating_review(int rating)
    {
        var decision = Review.Create(Id, TitleId, UserId, "Lan", rating, null, DateTimeOffset.UnixEpoch);

        Assert.False(decision.IsAccepted);
        Assert.Null(decision.Review);
        Assert.Equal(ReviewRejection.RatingOutOfRange, decision.Rejection);
    }

    [Fact]
    public void Create_rejects_blank_or_overlong_author()
    {
        var blank = Review.Create(Id, TitleId, UserId, "   ", 8, null, DateTimeOffset.UnixEpoch);
        var overlong = Review.Create(
            Id,
            TitleId,
            UserId,
            new string('a', Review.MaximumAuthorNameLength + 1),
            8,
            null,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(ReviewRejection.AuthorNameRequired, blank.Rejection);
        Assert.Equal(ReviewRejection.AuthorNameTooLong, overlong.Rejection);
    }

    [Fact]
    public void Create_accepts_author_and_comment_at_their_maximum_lengths()
    {
        var decision = Review.Create(
            Id,
            TitleId,
            UserId,
            new string('a', Review.MaximumAuthorNameLength),
            8,
            new string('c', Review.MaximumCommentLength),
            DateTimeOffset.UnixEpoch);

        Assert.True(decision.IsAccepted);
        Assert.Equal(Review.MaximumAuthorNameLength, decision.Review!.AuthorName.Length);
        Assert.Equal(Review.MaximumCommentLength, decision.Review.Comment!.Length);
    }

    [Fact]
    public void Create_rejects_comment_longer_than_normalized_limit()
    {
        var decision = Review.Create(
            Id,
            TitleId,
            UserId,
            "Lan",
            8,
            new string('a', Review.MaximumCommentLength + 1),
            DateTimeOffset.UnixEpoch);

        Assert.False(decision.IsAccepted);
        Assert.Equal(ReviewRejection.CommentTooLong, decision.Rejection);
    }

    [Fact]
    public void Edit_updates_content_and_updated_timestamp_but_retains_creation_timestamp()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 8, 15, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(1);
        var review = Review.Create(Id, TitleId, UserId, "Lan", 6, "Old", createdAt).Review!;

        var decision = review.Edit("Minh", 10, "  New  ", updatedAt);

        Assert.True(decision.IsAccepted);
        Assert.Same(review, decision.Review);
        Assert.Equal("Minh", review.AuthorName);
        Assert.Equal(10, review.Rating.Value);
        Assert.Equal("New", review.Comment);
        Assert.Equal(createdAt, review.CreatedAt);
        Assert.Equal(updatedAt, review.UpdatedAt);
    }

    [Fact]
    public void Edit_normalizes_blank_comment_to_null()
    {
        var review = Review.Create(Id, TitleId, UserId, "Lan", 6, "Old", DateTimeOffset.UnixEpoch).Review!;

        var decision = review.Edit("Lan", 6, "   ", DateTimeOffset.UnixEpoch.AddDays(1));

        Assert.True(decision.IsAccepted);
        Assert.Null(review.Comment);
    }

    [Fact]
    public void Rejected_edit_leaves_existing_state_unchanged()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 8, 15, 0, TimeSpan.Zero);
        var review = Review.Create(Id, TitleId, UserId, "Lan", 6, "Old", createdAt).Review!;

        var decision = review.Edit("Changed", Rating.Maximum + 1, "New", createdAt.AddDays(1));

        Assert.False(decision.IsAccepted);
        Assert.Equal(ReviewRejection.RatingOutOfRange, decision.Rejection);
        Assert.Equal("Lan", review.AuthorName);
        Assert.Equal(6, review.Rating.Value);
        Assert.Equal("Old", review.Comment);
        Assert.Equal(createdAt, review.CreatedAt);
        Assert.Equal(createdAt, review.UpdatedAt);
    }

    [Fact]
    public void Aggregate_has_only_a_private_parameterless_constructor_for_materialization()
    {
        var constructor = typeof(Review).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
        Assert.Empty(typeof(Review).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void Aggregate_state_can_only_be_changed_through_domain_behavior()
    {
        var stateProperties = typeof(Review).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.NotEmpty(stateProperties);
        Assert.All(stateProperties, property =>
        {
            Assert.NotNull(property.SetMethod);
            Assert.True(property.SetMethod.IsPrivate);
        });
    }
}
