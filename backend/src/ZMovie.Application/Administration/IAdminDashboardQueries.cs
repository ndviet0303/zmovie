namespace ZMovie.Application.Administration;

public interface IAdminDashboardQueries
{
    Task<AdminOverview> GetOverviewAsync(CancellationToken ct);
    Task<PagedResult<AdminTitleSummary>> ListTitlesAsync(AdminTitleFilter filter, CancellationToken ct);
    Task<AdminTitleDetail?> GetTitleAsync(string slug, CancellationToken ct);
    Task<PagedResult<AdminUserSummary>> ListUsersAsync(string? query, string? role, int page, int pageSize, CancellationToken ct);
    Task<AdminUserSummary?> GetUserAsync(Guid userId, CancellationToken ct);
    Task<int> CountAdminsAsync(CancellationToken ct);
    Task<PagedResult<AdminReviewSummary>> ListReviewsAsync(string? query, int? maxRating, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<AdminGenreSummary>> ListGenresAsync(CancellationToken ct);
}
