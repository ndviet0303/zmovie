using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;

namespace ZMovie.Application.Engagement;

public sealed record TitleReviewItem(Guid Id, string AuthorName, int Rating, string? Comment, DateTimeOffset UpdatedAt);
public sealed record TitleReviewsResponse(double AverageRating, int RatingCount, IReadOnlyList<TitleReviewItem> Items);
public sealed record ReviewEntry(Guid Id, string AuthorName, int Rating, string? Comment, DateTimeOffset UpdatedAt);

public interface ITitleReviewStore
{
    Task<IReadOnlyList<ReviewEntry>> GetAsync(Guid titleId, CancellationToken ct);
    Task UpsertAsync(Guid titleId, Guid userId, string authorName, int rating, string? comment, CancellationToken ct);
    Task<bool> RemoveReviewAsync(Guid titleId, Guid userId, CancellationToken ct);
}

public sealed record GetTitleReviewsQuery(string Slug) : IQuery<TitleReviewsResponse>;
public sealed class GetTitleReviewsHandler(ITitleReviewStore store, ILibraryCatalogReader catalog) : IRequestHandler<GetTitleReviewsQuery, ErrorOr<TitleReviewsResponse>>
{
    public async Task<ErrorOr<TitleReviewsResponse>> Handle(GetTitleReviewsQuery request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");
        var reviews = await store.GetAsync(titleId.Value, ct);
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
public sealed class SubmitTitleReviewHandler(ITitleReviewStore store, ILibraryCatalogReader catalog) : IRequestHandler<SubmitTitleReviewCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(SubmitTitleReviewCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");
        await store.UpsertAsync(titleId.Value, request.UserId, request.AuthorName, request.Rating, request.Comment?.Trim(), ct);
        return true;
    }
}

public sealed record RemoveTitleReviewCommand(Guid UserId, string Slug) : ICommand<bool>;
public sealed class RemoveTitleReviewHandler(ITitleReviewStore store, ILibraryCatalogReader catalog) : IRequestHandler<RemoveTitleReviewCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RemoveTitleReviewCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null || !await store.RemoveReviewAsync(titleId.Value, request.UserId, ct)) return Error.NotFound("engagement.review.not_found", "Review not found.");
        return true;
    }
}
