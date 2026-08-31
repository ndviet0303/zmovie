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




