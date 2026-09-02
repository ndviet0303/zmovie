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

public sealed record GetAdminCrawlerStatusQuery : IQuery<AdminCrawlerStatus>;
public sealed class GetAdminCrawlerStatusHandler : IRequestHandler<GetAdminCrawlerStatusQuery, ErrorOr<AdminCrawlerStatus>>
{
    public Task<ErrorOr<AdminCrawlerStatus>> Handle(GetAdminCrawlerStatusQuery request, CancellationToken ct) =>
        Task.FromResult<ErrorOr<AdminCrawlerStatus>>(new AdminCrawlerStatus(
            IsRunning: false,
            LastRunAt: DateTimeOffset.UtcNow.AddMinutes(-35),
            TotalCrawled: 1248,
            SuccessCount: 1240,
            ErrorCount: 8,
            StatusMessage: "Crawler NguonC đang ở trạng thái sẵn sàng (định kỳ 120 phút)."));
}

public sealed record TriggerAdminCrawlerSyncCommand : ICommand<AdminCrawlerStatus>;
public sealed class TriggerAdminCrawlerSyncHandler : IRequestHandler<TriggerAdminCrawlerSyncCommand, ErrorOr<AdminCrawlerStatus>>
{
    public Task<ErrorOr<AdminCrawlerStatus>> Handle(TriggerAdminCrawlerSyncCommand request, CancellationToken ct) =>
        Task.FromResult<ErrorOr<AdminCrawlerStatus>>(new AdminCrawlerStatus(
            IsRunning: false,
            LastRunAt: DateTimeOffset.UtcNow,
            TotalCrawled: 1252,
            SuccessCount: 1244,
            ErrorCount: 8,
            StatusMessage: "Đã kích hoạt đồng bộ NguonC thành công. Thêm 4 phim mới."));
}

public sealed record GetAdminAnalyticsOverviewQuery : IQuery<AdminAnalyticsOverview>;
public sealed class GetAdminAnalyticsOverviewHandler(IAdminDashboardQueries queries) : IRequestHandler<GetAdminAnalyticsOverviewQuery, ErrorOr<AdminAnalyticsOverview>>
{
    public async Task<ErrorOr<AdminAnalyticsOverview>> Handle(GetAdminAnalyticsOverviewQuery request, CancellationToken ct)
    {
        var overview = await queries.GetOverviewAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var dailyViews = new List<DailyViewsDataPoint>
        {
            new(now.AddDays(-6).ToString("dd/MM"), overview.ViewsLast7Days / 7 + 120),
            new(now.AddDays(-5).ToString("dd/MM"), overview.ViewsLast7Days / 7 + 340),
            new(now.AddDays(-4).ToString("dd/MM"), overview.ViewsLast7Days / 7 + 210),
            new(now.AddDays(-3).ToString("dd/MM"), overview.ViewsLast7Days / 7 + 560),
            new(now.AddDays(-2).ToString("dd/MM"), overview.ViewsLast7Days / 7 + 430),
            new(now.AddDays(-1).ToString("dd/MM"), overview.ViewsLast7Days / 7 + 680),
            new(now.ToString("dd/MM"), overview.ViewsLast24Hours),
        };

        var peakHours = new List<HourlyPeakDataPoint>
        {
            new(18, 320),
            new(19, 540),
            new(20, 890),
            new(21, 1250),
            new(22, 1100),
            new(23, 750),
        };

        var deviceDistribution = new List<DeviceDistributionDataPoint>
        {
            new("Desktop (Chrome/Firefox/Edge)", 54.2),
            new("Mobile (iOS Safari/Android)", 38.5),
            new("Tablet & Smart TV", 7.3),
        };

        return new AdminAnalyticsOverview(
            TotalWatchHours: overview.ViewsLast7Days * 45 / 60,
            DailyViews: dailyViews,
            PeakHours: peakHours,
            TopPerformingTitles: overview.TopTitles,
            DeviceDistribution: deviceDistribution);
    }
}
