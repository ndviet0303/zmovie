using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public sealed record TitleReviewItem(Guid Id, string AuthorName, int Rating, string? Comment, DateTimeOffset UpdatedAt);
public sealed record TitleReviewsResponse(double AverageRating, int RatingCount, IReadOnlyList<TitleReviewItem> Items);
public sealed record ReviewEntry(Guid Id, string AuthorName, int Rating, string? Comment, DateTimeOffset UpdatedAt);

public sealed record GetTitleReviewsQuery(string Slug) : IQuery<TitleReviewsResponse>;
public sealed class GetTitleReviewsHandler(IReviewQueries queries, ILibraryCatalogReader catalog) : IRequestHandler<GetTitleReviewsQuery, ErrorOr<TitleReviewsResponse>>
{
    public async Task<ErrorOr<TitleReviewsResponse>> Handle(GetTitleReviewsQuery request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");
        var reviews = await queries.ListByTitleAsync(new TitleId(titleId.Value), ct);
        return new TitleReviewsResponse(reviews.Count == 0 ? 0 : Math.Round(reviews.Average(x => x.Rating), 1), reviews.Count, reviews.Select(x => new TitleReviewItem(x.Id, x.AuthorName, x.Rating, x.Comment, x.UpdatedAt)).ToList());
    }
}

public sealed record SubmitTitleReviewCommand(Guid UserId, string AuthorName, string Slug, int Rating, string? Comment) : ICommand<bool>;
public sealed class SubmitTitleReviewValidator : AbstractValidator<SubmitTitleReviewCommand>
{
    public SubmitTitleReviewValidator()
    {
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Rating).InclusiveBetween(1, 10);
        RuleFor(x => x.Comment).MaximumLength(2_000);
    }
}
public sealed class SubmitTitleReviewHandler(
    IReviewRepository repository,
    ILibraryCatalogReader catalog,
    TimeProvider timeProvider) : IRequestHandler<SubmitTitleReviewCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(SubmitTitleReviewCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");

        var typedTitleId = new TitleId(titleId.Value);
        var typedUserId = new UserId(request.UserId);
        var review = await repository.FindByTitleAndUserAsync(typedTitleId, typedUserId, ct);
        var decision = review is null
            ? Review.Create(ReviewId.New(), typedTitleId, typedUserId, request.AuthorName, request.Rating, request.Comment, timeProvider.GetUtcNow())
            : review.Edit(request.AuthorName, request.Rating, request.Comment, timeProvider.GetUtcNow());

        if (!decision.IsAccepted) return ToValidationError(decision.Rejection!.Value);
        if (review is null) repository.Add(decision.Review!);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    private static Error ToValidationError(ReviewRejection rejection) => rejection switch
    {
        ReviewRejection.RatingOutOfRange => Error.Validation(nameof(SubmitTitleReviewCommand.Rating), "Rating must be between 1 and 10."),
        ReviewRejection.AuthorNameRequired => Error.Validation(nameof(SubmitTitleReviewCommand.AuthorName), "Author name is required."),
        ReviewRejection.AuthorNameTooLong => Error.Validation(nameof(SubmitTitleReviewCommand.AuthorName), "Author name is too long."),
        ReviewRejection.CommentTooLong => Error.Validation(nameof(SubmitTitleReviewCommand.Comment), "Comment is too long."),
        _ => Error.Unexpected("engagement.review.rejected", "Review was rejected."),
    };
}

public sealed record RemoveTitleReviewCommand(Guid UserId, string Slug) : ICommand<bool>;
public sealed class RemoveTitleReviewHandler(IReviewRepository repository, ILibraryCatalogReader catalog) : IRequestHandler<RemoveTitleReviewCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RemoveTitleReviewCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("engagement.review.not_found", "Review not found.");

        var review = await repository.FindByTitleAndUserAsync(new TitleId(titleId.Value), new UserId(request.UserId), ct);
        if (review is null) return Error.NotFound("engagement.review.not_found", "Review not found.");

        repository.Remove(review);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record DeleteReviewCommand(Guid ReviewId) : ICommand<bool>;
public sealed class DeleteReviewValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewValidator() => RuleFor(command => command.ReviewId).NotEmpty();
}

public sealed class DeleteReviewHandler(IReviewRepository repository) : IRequestHandler<DeleteReviewCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var review = await repository.FindByIdAsync(new ReviewId(request.ReviewId), ct);
        if (review is null) return Error.NotFound("admin.review.not_found", "Review not found.");

        repository.Remove(review);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
