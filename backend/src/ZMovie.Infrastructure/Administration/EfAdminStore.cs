using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZMovie.Application.Administration;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Administration;

public sealed class EfAdminStore(CatalogDbContext db) : IAdminStore
{
    private const int TopTitleCount = 8;
    private const int RecentUserCount = 8;
    private const int DeleteBatchSize = 500;

    public async Task<AdminOverview> GetOverviewAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);

        var titlesByType = await db.Titles.AsNoTracking()
            .GroupBy(x => x.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var featuredCount = await db.Titles.AsNoTracking().CountAsync(x => x.Featured, ct);
        var episodeCount = await db.Episodes.AsNoTracking().CountAsync(ct);
        var genreCount = await db.Genres.AsNoTracking().CountAsync(ct);

        var usersByRole = await db.Users.AsNoTracking()
            .GroupBy(x => x.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var reviewCount = await db.TitleReviews.AsNoTracking().CountAsync(ct);
        var averageRating = await db.TitleReviews.AsNoTracking().AverageAsync(x => (double?)x.Rating, ct) ?? 0;

        var views24Hours = await db.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.ViewedAt >= last24Hours, ct);
        var views7Days = await db.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.ViewedAt >= last7Days, ct);

        var topBuckets = await db.TitleViewEvents.AsNoTracking()
            .Where(x => x.ViewedAt >= last7Days)
            .GroupBy(x => x.TitleId)
            .Select(group => new { TitleId = group.Key, Views = group.LongCount() })
            .OrderByDescending(x => x.Views)
            .Take(TopTitleCount)
            .ToListAsync(ct);
        var topIds = topBuckets.Select(x => x.TitleId).ToList();
        var topTitleRows = await db.Titles.AsNoTracking()
            .Where(x => topIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Slug, x.VietnameseTitle, x.PosterUrl })
            .ToListAsync(ct);
        var topTitles = topBuckets
            .Join(topTitleRows, bucket => bucket.TitleId, row => row.Id, (bucket, row) => new AdminTopTitle(row.Slug, row.VietnameseTitle, row.PosterUrl, bucket.Views))
            .ToList();

        var recentUsers = await db.Users.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(RecentUserCount)
            .Select(x => new AdminUserSummary(x.Id, x.Email, x.DisplayName, x.AvatarUrl, x.Role, x.CreatedAt, x.LastSignedInAt))
            .ToListAsync(ct);

        return new AdminOverview(
            titlesByType.Sum(x => x.Count),
            titlesByType.Where(x => x.Type == AdminTitleTypes.Movie).Sum(x => x.Count),
            titlesByType.Where(x => x.Type == AdminTitleTypes.Series).Sum(x => x.Count),
            featuredCount,
            episodeCount,
            genreCount,
            usersByRole.Sum(x => x.Count),
            usersByRole.Where(x => x.Role == ZMovieRoles.Admin).Sum(x => x.Count),
            reviewCount,
            Math.Round(averageRating, 1),
            views24Hours,
            views7Days,
            topTitles,
            recentUsers);
    }

    public async Task<PagedResult<AdminTitleSummary>> ListTitlesAsync(AdminTitleFilter filter, CancellationToken ct)
    {
        var query = db.Titles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.ToLowerInvariant();
            query = query.Where(x => x.Slug.ToLower().Contains(term)
                || x.VietnameseTitle.ToLower().Contains(term)
                || x.EnglishTitle.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(filter.Genre))
        {
            // titles.genre holds a comma-joined list of every upstream category, which is why
            // the public catalog matches it with a substring predicate too. Exact equality here
            // would return nothing for any multi-category title.
            var genre = filter.Genre.ToLowerInvariant();
            query = query.Where(x => x.Genre.ToLower().Contains(genre));
        }
        if (!string.IsNullOrWhiteSpace(filter.Type)) query = query.Where(x => x.Type == filter.Type);
        if (filter.Featured is { } featured) query = query.Where(x => x.Featured == featured);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(Offset(filter.Page, filter.PageSize))
            .Take(filter.PageSize)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.VietnameseTitle,
                x.EnglishTitle,
                x.Genre,
                x.Year,
                x.Type,
                x.PosterUrl,
                x.RuntimeMinutes,
                x.Featured,
                x.UpdatedAt,
            })
            .ToListAsync(ct);

        var pageIds = rows.Select(x => x.Id).ToList();
        var episodeCounts = await db.Episodes.AsNoTracking()
            .Where(x => pageIds.Contains(x.TitleId))
            .GroupBy(x => x.TitleId)
            .Select(group => new { TitleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.TitleId, x => x.Count, ct);

        var items = rows.Select(x => new AdminTitleSummary(
            x.Id, x.Slug, x.VietnameseTitle, x.EnglishTitle, x.Genre, x.Year, x.Type, x.PosterUrl,
            x.RuntimeMinutes, x.Featured, episodeCounts.GetValueOrDefault(x.Id), x.UpdatedAt)).ToList();

        return new PagedResult<AdminTitleSummary>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<AdminTitleDetail?> GetTitleAsync(string slug, CancellationToken ct)
    {
        var title = await db.Titles.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, ct);
        return title is null ? null : await ToDetailAsync(title, ct);
    }

    public async Task<AdminTitleDetail?> UpdateTitleAsync(string slug, AdminTitleEdit edit, CancellationToken ct)
    {
        var title = await db.Titles.FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (title is null) return null;

        title.VietnameseTitle = edit.VietnameseTitle;
        title.EnglishTitle = edit.EnglishTitle;
        title.VietnameseSynopsis = edit.VietnameseSynopsis;
        title.EnglishSynopsis = edit.EnglishSynopsis;
        title.Genre = edit.Genre;
        title.Year = edit.Year;
        title.Type = edit.Type;
        title.PosterUrl = edit.PosterUrl;
        title.RuntimeMinutes = edit.RuntimeMinutes;
        title.Featured = edit.Featured;
        title.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveWithConcurrencyRetryAsync(title, ct);

        return await ToDetailAsync(title, ct);
    }

    public async Task<AdminTitleDetail?> SetTitleFeaturedAsync(string slug, bool featured, CancellationToken ct)
    {
        var title = await db.Titles.FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (title is null) return null;

        title.Featured = featured;
        title.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveWithConcurrencyRetryAsync(title, ct);

        return await ToDetailAsync(title, ct);
    }

    /// <summary>
    /// <c>titles.updated_at</c> is a concurrency token, so a crawler import touching the same
    /// row between the read and the write throws. An admin edit is an explicit, deliberate
    /// action, so it wins: refresh the token from the database and re-apply once instead of
    /// surfacing an unhandled 500.
    /// </summary>
    private async Task SaveWithConcurrencyRetryAsync(CatalogTitle title, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var entry = exception.Entries.SingleOrDefault(x => ReferenceEquals(x.Entity, title));
            if (entry is null) throw;

            var current = await entry.GetDatabaseValuesAsync(ct);
            if (current is null) throw; // Deleted underneath us — nothing left to update.

            entry.OriginalValues.SetValues(current);
            title.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> DeleteTitleAsync(string slug, CancellationToken ct)
    {
        var title = await db.Titles.FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (title is null) return false;

        // Without a transaction, a failure part-way leaves the title alive but stripped of
        // every episode and engagement row — worse than not deleting at all.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(ct)
            : null;

        // The engagement tables carry no foreign keys (see migration 202607230003), so the
        // dependent rows have to be removed explicitly or they are orphaned.
        await DeleteInBatchesAsync(db.TitleViewEvents.Where(x => x.TitleId == title.Id), ct);
        await DeleteInBatchesAsync(db.AssistantLearningEvents.Where(x => x.TitleId == title.Id), ct);
        await DeleteInBatchesAsync(db.WatchHistory.Where(x => x.TitleId == title.Id), ct);
        await DeleteInBatchesAsync(db.SavedTitles.Where(x => x.TitleId == title.Id), ct);
        await DeleteInBatchesAsync(db.TitleReviews.Where(x => x.TitleId == title.Id), ct);
        await DeleteInBatchesAsync(db.Episodes.Where(x => x.TitleId == title.Id), ct);

        db.Titles.Remove(title);
        await db.SaveChangesAsync(ct);

        if (transaction is not null) await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<PagedResult<AdminUserSummary>> ListUsersAsync(string? query, string? role, int page, int pageSize, CancellationToken ct)
    {
        var users = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.ToLowerInvariant();
            users = users.Where(x => x.Email.ToLower().Contains(term) || x.DisplayName.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(role)) users = users.Where(x => x.Role == role);

        var total = await users.CountAsync(ct);
        var items = await users
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(Offset(page, pageSize))
            .Take(pageSize)
            .Select(x => new AdminUserSummary(x.Id, x.Email, x.DisplayName, x.AvatarUrl, x.Role, x.CreatedAt, x.LastSignedInAt))
            .ToListAsync(ct);

        return new PagedResult<AdminUserSummary>(items, total, page, pageSize);
    }

    public async Task<AdminUserSummary?> GetUserAsync(Guid userId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new AdminUserSummary(x.Id, x.Email, x.DisplayName, x.AvatarUrl, x.Role, x.CreatedAt, x.LastSignedInAt))
            .FirstOrDefaultAsync(ct);

    public async Task<SetRoleResult> SetUserRoleAsync(Guid userId, string role, bool guardLastAdmin, CancellationToken ct)
    {
        // Serializable so the admin count and the write see one consistent snapshot: two
        // admins demoting each other at the same moment must not both observe "2 admins".
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return new SetRoleResult(SetRoleOutcome.NotFound, null);

        if (guardLastAdmin && await db.Users.CountAsync(x => x.Role == ZMovieRoles.Admin, ct) <= 1)
            return new SetRoleResult(SetRoleOutcome.LastAdmin, null);

        user.Role = ZMovieRoles.Normalize(role);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        return new SetRoleResult(
            SetRoleOutcome.Updated,
            new AdminUserSummary(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.Role, user.CreatedAt, user.LastSignedInAt));
    }

    public async Task<int> CountAdminsAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking().CountAsync(x => x.Role == ZMovieRoles.Admin, ct);

    public async Task<PagedResult<AdminReviewSummary>> ListReviewsAsync(string? query, int? maxRating, int page, int pageSize, CancellationToken ct)
    {
        var reviews = db.TitleReviews.AsNoTracking().AsQueryable();
        if (maxRating is { } rating) reviews = reviews.Where(x => x.Rating <= rating);
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
            .ToListAsync(ct);

        var titleIds = rows.Select(x => x.TitleId).Distinct().ToList();
        var titles = await db.Titles.AsNoTracking()
            .Where(x => titleIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Slug, x.VietnameseTitle })
            .ToDictionaryAsync(x => x.Id, x => x, ct);

        var items = rows.Select(x =>
        {
            var title = titles.GetValueOrDefault(x.TitleId);
            return new AdminReviewSummary(x.Id, title?.Slug ?? string.Empty, title?.VietnameseTitle ?? "(phim đã xoá)", x.UserId, x.AuthorName, x.Rating, x.Comment, x.UpdatedAt);
        }).ToList();

        return new PagedResult<AdminReviewSummary>(items, total, page, pageSize);
    }

    public async Task<bool> DeleteReviewAsync(Guid reviewId, CancellationToken ct)
    {
        var review = await db.TitleReviews.FirstOrDefaultAsync(x => x.Id == reviewId, ct);
        if (review is null) return false;

        db.TitleReviews.Remove(review);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AdminGenreSummary>> ListGenresAsync(CancellationToken ct)
    {
        var genres = await db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        if (genres.Count == 0) return [];

        // titles.genre is a comma-joined list of category names, not a foreign key, so a title
        // counts towards every genre whose name appears inside it. The whole distinct set of
        // stored values is small (one row per unique combination), so counting in memory keeps
        // this to a single query instead of one per genre.
        var storedGenres = await db.Titles.AsNoTracking()
            .GroupBy(x => x.Genre)
            .Select(group => new { Value = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        return genres.Select(genre => new AdminGenreSummary(
            genre.Id,
            genre.Slug,
            genre.Name,
            storedGenres.Where(x => ContainsGenre(x.Value, genre.Name)).Sum(x => x.Count),
            genre.UpdatedAt)).ToList();
    }

    private static bool ContainsGenre(string? storedValue, string genreName) =>
        !string.IsNullOrWhiteSpace(storedValue)
        && storedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => string.Equals(part, genreName, StringComparison.OrdinalIgnoreCase));

    public async Task<AdminGenreSummary?> CreateGenreAsync(string slug, string name, CancellationToken ct)
    {
        if (await db.Genres.AsNoTracking().AnyAsync(x => x.Slug == slug, ct)) return null;

        var genre = new CatalogGenre { Slug = slug, Name = name };
        db.Genres.Add(genre);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Lost the race against a concurrent create on ix_genres_slug. Any other write
            // failure is a real fault and must not be reported to the admin as "duplicate slug".
            db.Entry(genre).State = EntityState.Detached;
            return null;
        }

        return new AdminGenreSummary(genre.Id, genre.Slug, genre.Name, 0, genre.UpdatedAt);
    }

    public async Task<AdminGenreSummary?> UpdateGenreAsync(Guid id, string name, CancellationToken ct)
    {
        var genre = await db.Genres.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (genre is null) return null;

        var previousName = genre.Name;
        if (string.Equals(previousName, name, StringComparison.Ordinal))
            return new AdminGenreSummary(genre.Id, genre.Slug, genre.Name, await CountTitlesForGenreAsync(genre.Name, ct), genre.UpdatedAt);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(ct)
            : null;

        genre.Name = name;
        genre.UpdatedAt = DateTimeOffset.UtcNow;

        // titles.genre stores names, not ids, so a rename that only touched the genres row
        // would orphan every title still carrying the old name.
        var affected = await db.Titles.Where(x => x.Genre.Contains(previousName)).ToListAsync(ct);
        var renamedCount = 0;
        foreach (var title in affected)
        {
            var parts = title.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!parts.Any(part => string.Equals(part, previousName, StringComparison.OrdinalIgnoreCase))) continue;

            title.Genre = string.Join(", ", parts.Select(part =>
                string.Equals(part, previousName, StringComparison.OrdinalIgnoreCase) ? name : part));
            title.UpdatedAt = DateTimeOffset.UtcNow;
            renamedCount++;
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        return new AdminGenreSummary(genre.Id, genre.Slug, genre.Name, renamedCount, genre.UpdatedAt);
    }

    private async Task<int> CountTitlesForGenreAsync(string genreName, CancellationToken ct)
    {
        var stored = await db.Titles.AsNoTracking()
            .Where(x => x.Genre.Contains(genreName))
            .Select(x => x.Genre)
            .ToListAsync(ct);
        return stored.Count(value => ContainsGenre(value, genreName));
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public async Task<bool> DeleteGenreAsync(Guid id, CancellationToken ct)
    {
        var genre = await db.Genres.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (genre is null) return false;

        db.Genres.Remove(genre);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Guards against <c>(page - 1) * pageSize</c> overflowing int and producing a negative
    /// OFFSET for an absurd page number.
    /// </summary>
    private static int Offset(int page, int pageSize)
    {
        var offset = (long)(page - 1) * pageSize;
        return offset >= int.MaxValue ? int.MaxValue : (int)offset;
    }

    private async Task<AdminTitleDetail> ToDetailAsync(CatalogTitle title, CancellationToken ct)
    {
        var episodeCount = await db.Episodes.AsNoTracking().CountAsync(x => x.TitleId == title.Id, ct);
        var viewCount = await db.TitleViewEvents.AsNoTracking().LongCountAsync(x => x.TitleId == title.Id, ct);
        var reviewCount = await db.TitleReviews.AsNoTracking().CountAsync(x => x.TitleId == title.Id, ct);

        return new AdminTitleDetail(
            title.Id, title.Slug, title.VietnameseTitle, title.EnglishTitle, title.VietnameseSynopsis, title.EnglishSynopsis,
            title.Genre, title.Year, title.Type, title.PosterUrl, title.RuntimeMinutes, title.Featured,
            episodeCount, viewCount, reviewCount, title.CreatedAt, title.UpdatedAt);
    }

    /// <summary>
    /// Removes rows in bounded batches. <c>title_view_events</c> is append-only and can hold
    /// millions of rows for one title, so it must never be materialised in a single list.
    /// </summary>
    private async Task DeleteInBatchesAsync<TEntity>(IQueryable<TEntity> source, CancellationToken ct) where TEntity : class
    {
        while (true)
        {
            var batch = await source.Take(DeleteBatchSize).ToListAsync(ct);
            if (batch.Count == 0) return;

            db.Set<TEntity>().RemoveRange(batch);
            await db.SaveChangesAsync(ct);
            if (batch.Count < DeleteBatchSize) return;
        }
    }
}
