using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using Role = ZMovie.Domain.Identity.Role;

namespace ZMovie.Application.Administration;

public static class AdminPaging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static int NormalizePage(int? page) => page is null or < 1 ? 1 : page.Value;
    public static int NormalizePageSize(int? pageSize) => Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
}

public sealed record GetAdminOverviewQuery : IQuery<AdminOverview>;
public sealed class GetAdminOverviewHandler(IAdminDashboardQueries queries) : IRequestHandler<GetAdminOverviewQuery, ErrorOr<AdminOverview>>
{
    public async Task<ErrorOr<AdminOverview>> Handle(GetAdminOverviewQuery request, CancellationToken ct) => await queries.GetOverviewAsync(ct);
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
public sealed class ListAdminTitlesHandler(IAdminDashboardQueries queries) : IRequestHandler<ListAdminTitlesQuery, ErrorOr<PagedResult<AdminTitleSummary>>>
{
    public async Task<ErrorOr<PagedResult<AdminTitleSummary>>> Handle(ListAdminTitlesQuery request, CancellationToken ct) =>
        await queries.ListTitlesAsync(
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
public sealed class GetAdminTitleHandler(IAdminDashboardQueries queries) : IRequestHandler<GetAdminTitleQuery, ErrorOr<AdminTitleDetail>>
{
    public async Task<ErrorOr<AdminTitleDetail>> Handle(GetAdminTitleQuery request, CancellationToken ct) =>
        await queries.GetTitleAsync(request.Slug.Trim(), ct) is { } title
            ? title
            : Error.NotFound("admin.title.not_found", "Catalog title not found.");
}

public sealed record ListAdminUsersQuery(string? Query, string? Role, int? Page, int? PageSize) : IQuery<PagedResult<AdminUserSummary>>;
public sealed class ListAdminUsersValidator : AbstractValidator<ListAdminUsersQuery>
{
    public ListAdminUsersValidator()
    {
        RuleFor(x => x.Query).MaximumLength(320);
        RuleFor(x => x.Role).Must(Role.IsKnown).When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Role must be 'member' or 'admin'.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, AdminPaging.MaxPageSize).When(x => x.PageSize.HasValue);
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
    }
}
public sealed class ListAdminUsersHandler(IAdminDashboardQueries queries) : IRequestHandler<ListAdminUsersQuery, ErrorOr<PagedResult<AdminUserSummary>>>
{
    public async Task<ErrorOr<PagedResult<AdminUserSummary>>> Handle(ListAdminUsersQuery request, CancellationToken ct) =>
        await queries.ListUsersAsync(
            request.Query?.Trim(),
            string.IsNullOrWhiteSpace(request.Role) ? null : Role.Normalize(request.Role).Value,
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
public sealed class ListAdminReviewsHandler(IAdminDashboardQueries queries) : IRequestHandler<ListAdminReviewsQuery, ErrorOr<PagedResult<AdminReviewSummary>>>
{
    public async Task<ErrorOr<PagedResult<AdminReviewSummary>>> Handle(ListAdminReviewsQuery request, CancellationToken ct) =>
        await queries.ListReviewsAsync(
            request.Query?.Trim(),
            request.MaxRating,
            AdminPaging.NormalizePage(request.Page),
            AdminPaging.NormalizePageSize(request.PageSize),
            ct);
}

public sealed record ListAdminGenresQuery : IQuery<List<AdminGenreSummary>>;
public sealed class ListAdminGenresHandler(IAdminDashboardQueries queries) : IRequestHandler<ListAdminGenresQuery, ErrorOr<List<AdminGenreSummary>>>
{
    public async Task<ErrorOr<List<AdminGenreSummary>>> Handle(ListAdminGenresQuery request, CancellationToken ct) =>
        (await queries.ListGenresAsync(ct)).ToList();
}
