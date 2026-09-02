using ZMovie.Domain.Common;

namespace ZMovie.Domain.Engagement;

public readonly record struct ReviewId(Guid Value) : IComparable<ReviewId>, IComparable
{
    public static ReviewId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(ReviewId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is ReviewId other ? CompareTo(other) : 1;
}

public readonly record struct UserId(Guid Value) : IComparable<UserId>, IComparable
{
    public static UserId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(UserId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is UserId other ? CompareTo(other) : 1;
}

public readonly record struct TitleId(Guid Value) : IComparable<TitleId>, IComparable
{
    public static TitleId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
    public int CompareTo(TitleId other) => Value.CompareTo(other.Value);
    public int CompareTo(object? obj) => obj is TitleId other ? CompareTo(other) : 1;
}

public readonly record struct Rating
{
    public const int Minimum = 1;
    public const int Maximum = 10;

    private Rating(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static bool TryCreate(int value, out Rating rating)
    {
        if (value is < Minimum or > Maximum)
        {
            rating = default;
            return false;
        }

        rating = new Rating(value);
        return true;
    }
}

public enum ReviewRejection
{
    RatingOutOfRange,
    AuthorNameRequired,
    AuthorNameTooLong,
    CommentTooLong,
}

public readonly record struct ReviewDecision
{
    private ReviewDecision(Review? review, ReviewRejection? rejection)
    {
        Review = review;
        Rejection = rejection;
    }

    public bool IsAccepted => Review is not null && Rejection is null;
    public Review? Review { get; }
    public ReviewRejection? Rejection { get; }

    internal static ReviewDecision Accept(Review review) => new(review, null);

    internal static ReviewDecision Reject(ReviewRejection rejection) => new(null, rejection);
}

public sealed class Review : AggregateRoot, IEntity<ReviewId>
{
    public const int MaximumAuthorNameLength = 300;
    public const int MaximumCommentLength = 2_000;

    private Review()
    {
        AuthorName = string.Empty;
    }

    private Review(
        ReviewId id,
        TitleId titleId,
        UserId userId,
        string authorName,
        Rating rating,
        string? comment,
        DateTimeOffset occurredAt)
    {
        Id = id;
        TitleId = titleId;
        UserId = userId;
        AuthorName = authorName;
        Rating = rating;
        Comment = comment;
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    public ReviewId Id { get; private set; }
    public TitleId TitleId { get; private set; }
    public UserId UserId { get; private set; }
    public string AuthorName { get; private set; }
    public Rating Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ReviewDecision Create(
        ReviewId id,
        TitleId titleId,
        UserId userId,
        string authorName,
        int rating,
        string? comment,
        DateTimeOffset occurredAt)
    {
        var rejection = Validate(authorName, rating, comment, out var validatedRating, out var normalizedComment);
        if (rejection is { } reason)
        {
            return ReviewDecision.Reject(reason);
        }

        var review = new Review(id, titleId, userId, authorName, validatedRating, normalizedComment, occurredAt);
        review.RaiseDomainEvent(new ReviewSubmittedDomainEvent(id, titleId, userId, validatedRating.Value, occurredAt));
        return ReviewDecision.Accept(review);
    }

    public ReviewDecision Edit(string authorName, int rating, string? comment, DateTimeOffset occurredAt)
    {
        var rejection = Validate(authorName, rating, comment, out var validatedRating, out var normalizedComment);
        if (rejection is { } reason)
        {
            return ReviewDecision.Reject(reason);
        }

        var oldRating = Rating.Value;
        AuthorName = authorName;
        Rating = validatedRating;
        Comment = normalizedComment;
        UpdatedAt = occurredAt;

        RaiseDomainEvent(new ReviewEditedDomainEvent(Id, TitleId, UserId, oldRating, validatedRating.Value, occurredAt));
        return ReviewDecision.Accept(this);
    }

    private static ReviewRejection? Validate(
        string authorName,
        int rating,
        string? comment,
        out Rating validatedRating,
        out string? normalizedComment)
    {
        normalizedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        if (!Rating.TryCreate(rating, out validatedRating))
        {
            return ReviewRejection.RatingOutOfRange;
        }

        if (string.IsNullOrWhiteSpace(authorName))
        {
            return ReviewRejection.AuthorNameRequired;
        }

        if (authorName.Length > MaximumAuthorNameLength)
        {
            return ReviewRejection.AuthorNameTooLong;
        }

        return normalizedComment?.Length > MaximumCommentLength
            ? ReviewRejection.CommentTooLong
            : null;
    }
}
