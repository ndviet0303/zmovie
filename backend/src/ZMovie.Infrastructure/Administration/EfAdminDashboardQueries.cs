using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Administration;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Engagement;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;

namespace ZMovie.Infrastructure.Administration;

public sealed class EfAdminDashboardQueries(
    CatalogDbContext catalog,
    IdentityDbContext identity,
    EngagementDbContext engagement,
    AnalyticsDbContext analytics) : IAdminDashboardQueries
{
    private const int TopTitleCount = 8;
    private const int RecentUserCount = 8;

    public async Task<AdminOverview> GetOverviewAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);

        var titlesByType = await catalog.Titles.AsNoTracking()
            .GroupBy(x => x.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var featuredCount = await catalog.Titles.AsNoTracking().CountAsync(x => x.Featured, ct);
        var episodeCount = await catalog.Episodes.AsNoTracking().CountAsync(ct);
        var genreCount = await catalog.Genres.AsNoTracking().CountAsync(ct);

        var usersByRole = await identity.Users.AsNoTracking()
            .GroupBy(x => x.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var reviewRatingBuckets = await engagement.TitleReviews.AsNoTracking()
            .GroupBy(x => x.Rating)
            .Select(group => new { Rating = group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var reviewCount = reviewRatingBuckets.Sum(x => x.Count);
        var averageRating = reviewCount == 0
            ? 0
            : reviewRatingBuckets.Sum(x => x.Rating.Value * x.Count) / (double)reviewCount;

        var views24Hours = await analytics.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.ViewedAt >= last24Hours, ct);
        var views7Days = await analytics.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.ViewedAt >= last7Days, ct);

        var topBuckets = await analytics.TitleViewEvents.AsNoTracking()
            .Where(x => x.ViewedAt >= last7Days)
            .GroupBy(x => x.TitleId)
            .Select(group => new { TitleId = group.Key, Views = group.LongCount() })
            .OrderByDescending(x => x.Views)
            .Take(TopTitleCount)
            .ToListAsync(ct);

        var topIds = topBuckets.Select(x => new Domain.Catalog.TitleId(x.TitleId.Value)).ToList();
        var topTitleRows = await catalog.Titles.AsNoTracking()
            .Where(x => topIds.Contains(x.Id))
            .Select(x => new { x.Id, Slug = x.Slug.Value, VietnameseTitle = x.VietnameseTitle, x.PosterUrl })
            .ToListAsync(ct);
        var topTitles = topBuckets
            .Join(topTitleRows, bucket => bucket.TitleId.Value, row => row.Id.Value, (bucket, row) => new AdminTopTitle(row.Slug, row.VietnameseTitle, row.PosterUrl, bucket.Views))
            .ToList();

        var recentUsers = await identity.Users.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(RecentUserCount)
            .Select(x => new AdminUserSummary(x.Id.Value, x.Email, x.DisplayName, x.AvatarUrl, x.Role.Value, x.CreatedAt, x.LastSignedInAt))
            .ToListAsync(ct);

        return new AdminOverview(
            titlesByType.Sum(x => x.Count),
            titlesByType.Where(x => x.Type.Value == AdminTitleTypes.Movie).Sum(x => x.Count),
            titlesByType.Where(x => x.Type.Value == AdminTitleTypes.Series).Sum(x => x.Count),
            featuredCount,
            episodeCount,
            genreCount,
            usersByRole.Sum(x => x.Count),
            usersByRole.Where(x => x.Role == Role.Admin).Sum(x => x.Count),
            reviewCount,
            Math.Round(averageRating, 1),
            views24Hours,
            views7Days,
            topTitles,
            recentUsers);
    }

    public async Task<PagedResult<AdminTitleSummary>> ListTitlesAsync(AdminTitleFilter filter, CancellationToken ct)
    {
        var query = catalog.Titles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.ToLowerInvariant();
            query = query.Where(x => x.Slug.Value.ToLower().Contains(term)
                || x.VietnameseTitle.ToLower().Contains(term)
                || x.EnglishTitle.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(filter.Genre))
        {
            var genre = filter.Genre.ToLowerInvariant();
            query = query.Where(x => x.Genre.ToLower().Contains(genre));
        }
        if (!string.IsNullOrWhiteSpace(filter.Type) && TitleType.TryCreate(filter.Type, out var filterType))
        {
            query = query.Where(x => x.Type == filterType);
        }
        if (filter.Featured is { } featured) query = query.Where(x => x.Featured == featured);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(Offset(filter.Page, filter.PageSize))
            .Take(filter.PageSize)
            .Select(x => new
            {
                Id = x.Id.Value,
                Slug = x.Slug.Value,
                VietnameseTitle = x.VietnameseTitle,
                EnglishTitle = x.EnglishTitle,
                x.Genre,
                Year = x.Year.Value,
                Type = x.Type.Value,
                x.PosterUrl,
                RuntimeMinutes = x.Runtime.Minutes,
                x.Featured,
                x.UpdatedAt,
            })
            .ToListAsync(ct);

        var pageIds = rows.Select(x => new Domain.Catalog.TitleId(x.Id)).ToList();
        var episodeCounts = await catalog.Episodes.AsNoTracking()
            .Where(x => pageIds.Contains(x.TitleId))
            .GroupBy(x => x.TitleId)
            .Select(group => new { TitleId = group.Key.Value, Count = group.Count() })
            .ToDictionaryAsync(x => x.TitleId, x => x.Count, ct);

        var items = rows.Select(x => new AdminTitleSummary(
            x.Id, x.Slug, x.VietnameseTitle, x.EnglishTitle, x.Genre, x.Year, x.Type, x.PosterUrl,
            x.RuntimeMinutes, x.Featured, episodeCounts.GetValueOrDefault(x.Id), x.UpdatedAt)).ToList();

        return new PagedResult<AdminTitleSummary>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<AdminTitleDetail?> GetTitleAsync(string slug, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        var title = await catalog.Titles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == titleSlug, ct);
        return title is null ? null : await ToDetailAsync(title, ct);
    }

    public async Task<PagedResult<AdminUserSummary>> ListUsersAsync(string? query, string? role, int page, int pageSize, CancellationToken ct)
    {
        var users = identity.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.ToLowerInvariant();
            users = users.Where(x => x.Email.ToLower().Contains(term) || x.DisplayName.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(role) && Role.TryCreate(role, out var parsedRole))
        {
            users = users.Where(x => x.Role == parsedRole);
        }

        var total = await users.CountAsync(ct);
        var items = await users
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(Offset(page, pageSize))
            .Take(pageSize)
            .Select(x => new AdminUserSummary(x.Id.Value, x.Email, x.DisplayName, x.AvatarUrl, x.Role.Value, x.CreatedAt, x.LastSignedInAt))
            .ToListAsync(ct);

        return new PagedResult<AdminUserSummary>(items, total, page, pageSize);
    }

    public async Task<AdminUserSummary?> GetUserAsync(Guid userId, CancellationToken ct) =>
        await identity.Users.AsNoTracking()
            .Where(x => x.Id == new Domain.Identity.UserId(userId))
            .Select(x => new AdminUserSummary(x.Id.Value, x.Email, x.DisplayName, x.AvatarUrl, x.Role.Value, x.CreatedAt, x.LastSignedInAt))
            .FirstOrDefaultAsync(ct);

    public async Task<int> CountAdminsAsync(CancellationToken ct) =>
        await identity.Users.AsNoTracking().CountAsync(x => x.Role == Role.Admin, ct);

    public async Task<PagedResult<AdminReviewSummary>> ListReviewsAsync(string? query, int? maxRating, int page, int pageSize, CancellationToken ct)
    {
        var reviews = engagement.TitleReviews.AsNoTracking().AsQueryable();
        if (maxRating is { } rating)
        {
            var allowedRatings = RatingsAtMost(rating);
            reviews = reviews.Where(x => allowedRatings.Contains(x.Rating));
        }
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.ToLowerInvariant();
            reviews = reviews.Where(x => x.AuthorName.ToLower().Contains(term)
                || (x.Comment != null && x.Comment.ToLower().Contains(term)));
        }

        var total = await reviews.CountAsync(ct);
        var rows = await reviews
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(Offset(page, pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TitleId,
                x.UserId,
                x.AuthorName,
                x.Rating,
                x.Comment,
                x.UpdatedAt,
            })
            .ToListAsync(ct);

        var titleIds = rows.Select(x => new Domain.Catalog.TitleId(x.TitleId.Value)).Distinct().ToList();
        var titles = await catalog.Titles.AsNoTracking()
            .Where(x => titleIds.Contains(x.Id))
            .Select(x => new { Id = x.Id.Value, Slug = x.Slug.Value, VietnameseTitle = x.VietnameseTitle })
            .ToDictionaryAsync(x => x.Id, x => x, ct);

        var items = rows.Select(x =>
        {
            var title = titles.GetValueOrDefault(x.TitleId.Value);
            return new AdminReviewSummary(x.Id.Value, title?.Slug ?? string.Empty, title?.VietnameseTitle ?? "(phim đã xoá)", x.UserId.Value, x.AuthorName, x.Rating.Value, x.Comment, x.UpdatedAt);
        }).ToList();

        return new PagedResult<AdminReviewSummary>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<AdminGenreSummary>> ListGenresAsync(CancellationToken ct)
    {
        var genres = await catalog.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        if (genres.Count == 0) return [];

        var storedGenres = await catalog.Titles.AsNoTracking()
            .GroupBy(x => x.Genre)
            .Select(group => new { Value = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        return genres.Select(genre => new AdminGenreSummary(
            genre.Id.Value,
            genre.Slug,
            genre.Name,
            storedGenres.Where(x => ContainsGenre(x.Value, genre.Name)).Sum(x => x.Count),
            genre.UpdatedAt)).ToList();
    }

    public async Task<AdminTitleDetail> ToDetailAsync(Title title, CancellationToken ct)
    {
        var episodeCount = await catalog.Episodes.AsNoTracking().CountAsync(x => x.TitleId == title.Id, ct);
        var viewCount = await analytics.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.TitleId == new Domain.Analytics.TitleId(title.Id.Value), ct);
        var reviewCount = await engagement.TitleReviews.AsNoTracking().CountAsync(x => x.TitleId == new Domain.Engagement.TitleId(title.Id.Value), ct);

        return new AdminTitleDetail(
            title.Id.Value, title.Slug.Value, title.VietnameseTitle, title.EnglishTitle, title.VietnameseSynopsis, title.EnglishSynopsis,
            title.Genre, title.Year.Value, title.Type.Value, title.PosterUrl, title.Runtime.Minutes, title.Featured,
            episodeCount, viewCount, reviewCount, title.CreatedAt, title.UpdatedAt);
    }

    private static int Offset(int page, int pageSize)
    {
        var offset = (long)(page - 1) * pageSize;
        return offset >= int.MaxValue ? int.MaxValue : (int)offset;
    }

    private static bool ContainsGenre(string? storedValue, string genreName) =>
        !string.IsNullOrWhiteSpace(storedValue)
        && storedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => string.Equals(part, genreName, StringComparison.OrdinalIgnoreCase));

    private static Rating[] RatingsAtMost(int maximum) =>
        Enumerable.Range(Rating.Minimum, maximum - Rating.Minimum + 1)
            .Select(value =>
            {
                _ = Rating.TryCreate(value, out var rating);
                return rating;
            })
            .ToArray();
}
