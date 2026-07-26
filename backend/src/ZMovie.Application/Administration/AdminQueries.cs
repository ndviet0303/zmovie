using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Identity;

namespace ZMovie.Application.Administration;

public static class AdminPaging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalizePage(int? page) => page is null or < 1 ? 1 : page.Value;
    public static int NormalizePageSize(int? pageSize) => Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
}

public sealed record GetAdminOverviewQuery : IQuery<AdminOverview>;
public sealed class GetAdminOverviewHandler(IAdminStore store) : IRequestHandler<GetAdminOverviewQuery, ErrorOr<AdminOverview>>
{
    public async Task<ErrorOr<AdminOverview>> Handle(GetAdminOverviewQuery request, CancellationToken ct) => await store.GetOverviewAsync(ct);
}

public sealed record ListAdminTitlesQuery(string? Query, string? Genre, string? Type, bool? Featured, int? Page, int? PageSize) : IQuery<PagedResult<AdminTitleSummary>>;
public sealed class ListAdminTitlesValidator : AbstractValidator<ListAdminTitlesQuery>
{
    public ListAdminTitlesValidator()
    {
        RuleFor(x => x.Query).MaximumLength(200);
        RuleFor(x => x.Genre).MaximumLength(100);
        RuleFor(x => x.Type).MaximumLength(32);
        RuleFor(x => x.PageSize).InclusiveBetween(1, AdminPaging.MaxPageSize).When(x => x.PageSize.HasValue);
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
    }
}
public sealed class ListAdminTitlesHandler(IAdminStore store) : IRequestHandler<ListAdminTitlesQuery, ErrorOr<PagedResult<AdminTitleSummary>>>
{
    public async Task<ErrorOr<PagedResult<AdminTitleSummary>>> Handle(ListAdminTitlesQuery request, CancellationToken ct) =>
        await store.ListTitlesAsync(
            new AdminTitleFilter(
                request.Query?.Trim(),
                request.Genre?.Trim(),
                request.Type?.Trim(),
                request.Featured,
                AdminPaging.NormalizePage(request.Page),
                AdminPaging.NormalizePageSize(request.PageSize)),
            ct);
}

public sealed record GetAdminTitleQuery(string Slug) : IQuery<AdminTitleDetail>;
public sealed class GetAdminTitleValidator : AbstractValidator<GetAdminTitleQuery>
{
    public GetAdminTitleValidator() => RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
}
public sealed class GetAdminTitleHandler(IAdminStore store) : IRequestHandler<GetAdminTitleQuery, ErrorOr<AdminTitleDetail>>
{
    public async Task<ErrorOr<AdminTitleDetail>> Handle(GetAdminTitleQuery request, CancellationToken ct) =>
        await store.GetTitleAsync(request.Slug.Trim(), ct) is { } title
            ? title
            : Error.NotFound("admin.title.not_found", "Catalog title not found.");
}

public sealed record ListAdminUsersQuery(string? Query, string? Role, int? Page, int? PageSize) : IQuery<PagedResult<AdminUserSummary>>;
public sealed class ListAdminUsersValidator : AbstractValidator<ListAdminUsersQuery>
{
    public ListAdminUsersValidator()
    {
        RuleFor(x => x.Query).MaximumLength(320);
        RuleFor(x => x.Role).Must(ZMovieRoles.IsKnown).When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Role must be 'member' or 'admin'.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, AdminPaging.MaxPageSize).When(x => x.PageSize.HasValue);
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
    }
}
public sealed class ListAdminUsersHandler(IAdminStore store) : IRequestHandler<ListAdminUsersQuery, ErrorOr<PagedResult<AdminUserSummary>>>
{
    public async Task<ErrorOr<PagedResult<AdminUserSummary>>> Handle(ListAdminUsersQuery request, CancellationToken ct) =>
        await store.ListUsersAsync(
            request.Query?.Trim(),
            string.IsNullOrWhiteSpace(request.Role) ? null : ZMovieRoles.Normalize(request.Role),
            AdminPaging.NormalizePage(request.Page),
            AdminPaging.NormalizePageSize(request.PageSize),
            ct);
}

public sealed record ListAdminReviewsQuery(string? Query, int? MaxRating, int? Page, int? PageSize) : IQuery<PagedResult<AdminReviewSummary>>;
public sealed class ListAdminReviewsValidator : AbstractValidator<ListAdminReviewsQuery>
{
    public ListAdminReviewsValidator()
    {
        RuleFor(x => x.Query).MaximumLength(300);
        RuleFor(x => x.MaxRating).InclusiveBetween(1, 10).When(x => x.MaxRating.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, AdminPaging.MaxPageSize).When(x => x.PageSize.HasValue);
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
    }
}
public sealed class ListAdminReviewsHandler(IAdminStore store) : IRequestHandler<ListAdminReviewsQuery, ErrorOr<PagedResult<AdminReviewSummary>>>
{
    public async Task<ErrorOr<PagedResult<AdminReviewSummary>>> Handle(ListAdminReviewsQuery request, CancellationToken ct) =>
        await store.ListReviewsAsync(
            request.Query?.Trim(),
            request.MaxRating,
            AdminPaging.NormalizePage(request.Page),
            AdminPaging.NormalizePageSize(request.PageSize),
            ct);
}

public sealed record ListAdminGenresQuery : IQuery<List<AdminGenreSummary>>;
public sealed class ListAdminGenresHandler(IAdminStore store) : IRequestHandler<ListAdminGenresQuery, ErrorOr<List<AdminGenreSummary>>>
{
    public async Task<ErrorOr<List<AdminGenreSummary>>> Handle(ListAdminGenresQuery request, CancellationToken ct) =>
        (await store.ListGenresAsync(ct)).ToList();
}
