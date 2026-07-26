namespace ZMovie.Application.Administration;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int PageCount => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record AdminTopTitle(string Slug, string Title, string PosterUrl, long Views);

public sealed record AdminOverview(
    int TitleCount,
    int MovieCount,
    int SeriesCount,
    int FeaturedCount,
    int EpisodeCount,
    int GenreCount,
    int UserCount,
    int AdminCount,
    int ReviewCount,
    double AverageRating,
    long ViewsLast24Hours,
    long ViewsLast7Days,
    IReadOnlyList<AdminTopTitle> TopTitles,
    IReadOnlyList<AdminUserSummary> RecentUsers);

public sealed record AdminTitleSummary(
    Guid Id,
    string Slug,
    string VietnameseTitle,
    string EnglishTitle,
    string Genre,
    int Year,
    string Type,
    string PosterUrl,
    int RuntimeMinutes,
    bool Featured,
    int EpisodeCount,
    DateTimeOffset UpdatedAt);

public sealed record AdminTitleDetail(
    Guid Id,
    string Slug,
    string VietnameseTitle,
    string EnglishTitle,
    string VietnameseSynopsis,
    string EnglishSynopsis,
    string Genre,
    int Year,
    string Type,
    string PosterUrl,
    int RuntimeMinutes,
    bool Featured,
    int EpisodeCount,
    long ViewCount,
    int ReviewCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminTitleEdit(
    string VietnameseTitle,
    string EnglishTitle,
    string VietnameseSynopsis,
    string EnglishSynopsis,
    string Genre,
    int Year,
    string Type,
    string PosterUrl,
    int RuntimeMinutes,
    bool Featured);

public sealed record AdminTitleFilter(string? Query, string? Genre, string? Type, bool? Featured, int Page, int PageSize);

public sealed record AdminUserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSignedInAt);

public sealed record AdminReviewSummary(
    Guid Id,
    string TitleSlug,
    string TitleName,
    Guid UserId,
    string AuthorName,
    int Rating,
    string? Comment,
    DateTimeOffset UpdatedAt);

public sealed record AdminGenreSummary(Guid Id, string Slug, string Name, int TitleCount, DateTimeOffset UpdatedAt);

public enum SetRoleOutcome
{
    Updated,
    NotFound,
    /// <summary>Refused because it would leave the system with no admin at all.</summary>
    LastAdmin,
}

public sealed record SetRoleResult(SetRoleOutcome Outcome, AdminUserSummary? User);

public interface IAdminStore
{
    Task<AdminOverview> GetOverviewAsync(CancellationToken ct);

    Task<PagedResult<AdminTitleSummary>> ListTitlesAsync(AdminTitleFilter filter, CancellationToken ct);
    Task<AdminTitleDetail?> GetTitleAsync(string slug, CancellationToken ct);
    Task<AdminTitleDetail?> UpdateTitleAsync(string slug, AdminTitleEdit edit, CancellationToken ct);
    Task<AdminTitleDetail?> SetTitleFeaturedAsync(string slug, bool featured, CancellationToken ct);
    Task<bool> DeleteTitleAsync(string slug, CancellationToken ct);

    Task<PagedResult<AdminUserSummary>> ListUsersAsync(string? query, string? role, int page, int pageSize, CancellationToken ct);
    Task<AdminUserSummary?> GetUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Sets the role. When <paramref name="guardLastAdmin"/> is set the "at least one admin
    /// must remain" check runs in the same transaction as the write, so two admins demoting
    /// each other concurrently cannot both pass a stale count.
    /// </summary>
    Task<SetRoleResult> SetUserRoleAsync(Guid userId, string role, bool guardLastAdmin, CancellationToken ct);
    Task<int> CountAdminsAsync(CancellationToken ct);

    Task<PagedResult<AdminReviewSummary>> ListReviewsAsync(string? query, int? maxRating, int page, int pageSize, CancellationToken ct);
    Task<bool> DeleteReviewAsync(Guid reviewId, CancellationToken ct);

    Task<IReadOnlyList<AdminGenreSummary>> ListGenresAsync(CancellationToken ct);
    Task<AdminGenreSummary?> CreateGenreAsync(string slug, string name, CancellationToken ct);
    Task<AdminGenreSummary?> UpdateGenreAsync(Guid id, string name, CancellationToken ct);
    Task<bool> DeleteGenreAsync(Guid id, CancellationToken ct);
}
