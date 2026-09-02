using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZMovie.Application.Administration;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Administration;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfCatalogAdministrationService(
    CatalogDbContext catalog,
    EfAdminDashboardQueries queries) : ICatalogAdministrationService
{
    private const int DeleteBatchSize = 500;

    public async Task<AdminTitleDetail?> UpdateTitleAsync(string slug, AdminTitleEdit edit, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        var title = await catalog.Titles.FirstOrDefaultAsync(x => x.Slug == titleSlug, ct);
        if (title is null) return null;

        var now = DateTimeOffset.UtcNow;
        title.UpdateMetadata(
            new LocalizedText(edit.VietnameseTitle, edit.EnglishTitle),
            new LocalizedText(edit.VietnameseSynopsis, edit.EnglishSynopsis),
            edit.Genre,
            ReleaseYear.FromInt(edit.Year),
            TitleType.Normalize(edit.Type),
            edit.PosterUrl,
            Runtime.FromMinutes(edit.RuntimeMinutes),
            edit.Featured,
            now);

        await SaveWithConcurrencyRetryAsync(title, ct);
        return await queries.ToDetailAsync(title, ct);
    }

    public async Task<AdminTitleDetail?> SetTitleFeaturedAsync(string slug, bool featured, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        var title = await catalog.Titles.FirstOrDefaultAsync(x => x.Slug == titleSlug, ct);
        if (title is null) return null;

        title.SetFeatured(featured, DateTimeOffset.UtcNow);
        await SaveWithConcurrencyRetryAsync(title, ct);
        return await queries.ToDetailAsync(title, ct);
    }

    public async Task<AdminGenreSummary?> CreateGenreAsync(string slug, string name, CancellationToken ct)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (await catalog.Genres.AsNoTracking().AnyAsync(x => x.Slug == normalizedSlug, ct)) return null;

        var now = DateTimeOffset.UtcNow;
        var genre = Genre.Create(GenreId.New(), normalizedSlug, name, now);
        catalog.Genres.Add(genre);
        try
        {
            await catalog.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            catalog.Entry(genre).State = EntityState.Detached;
            return null;
        }

        return new AdminGenreSummary(genre.Id.Value, genre.Slug, genre.Name, 0, genre.UpdatedAt);
    }

    public async Task<AdminGenreSummary?> UpdateGenreAsync(Guid id, string name, CancellationToken ct)
    {
        var genreId = new GenreId(id);
        var genre = await catalog.Genres.FirstOrDefaultAsync(x => x.Id == genreId, ct);
        if (genre is null) return null;

        var previousName = genre.Name;
        if (string.Equals(previousName, name, StringComparison.Ordinal))
            return new AdminGenreSummary(genre.Id.Value, genre.Slug, genre.Name, await CountTitlesForGenreAsync(genre.Name, ct), genre.UpdatedAt);

        await using var transaction = catalog.Database.IsNpgsql()
            ? await catalog.Database.BeginTransactionAsync(ct)
            : null;

        var now = DateTimeOffset.UtcNow;
        genre.Rename(name, now);

        var affected = await catalog.Titles.Where(x => x.Genre.Contains(previousName)).ToListAsync(ct);
        var renamedCount = 0;
        foreach (var title in affected)
        {
            var parts = title.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!parts.Any(part => string.Equals(part, previousName, StringComparison.OrdinalIgnoreCase))) continue;

            var newGenre = string.Join(", ", parts.Select(part =>
                string.Equals(part, previousName, StringComparison.OrdinalIgnoreCase) ? name : part));
            title.UpdateMetadata(
                title.TitleName,
                title.Synopsis,
                newGenre,
                title.Year,
                title.Type,
                title.PosterUrl,
                title.Runtime,
                title.Featured,
                now);
            renamedCount++;
        }

        await catalog.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        return new AdminGenreSummary(genre.Id.Value, genre.Slug, genre.Name, renamedCount, genre.UpdatedAt);
    }

    public async Task<bool> DeleteGenreAsync(Guid id, CancellationToken ct)
    {
        var genreId = new GenreId(id);
        var genre = await catalog.Genres.FirstOrDefaultAsync(x => x.Id == genreId, ct);
        if (genre is null) return false;

        await DeleteInBatchesAsync(catalog.TitleGenres.Where(x => x.GenreId == genreId), catalog, ct);
        catalog.Genres.Remove(genre);
        await catalog.SaveChangesAsync(ct);
        return true;
    }

    private async Task SaveWithConcurrencyRetryAsync(Title title, CancellationToken ct)
    {
        try
        {
            await catalog.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var entry = exception.Entries.SingleOrDefault(x => ReferenceEquals(x.Entity, title));
            if (entry is null) throw;

            var current = await entry.GetDatabaseValuesAsync(ct);
            if (current is null) throw;

            entry.OriginalValues.SetValues(current);
            title.SetFeatured(title.Featured, DateTimeOffset.UtcNow);
            await catalog.SaveChangesAsync(ct);
        }
    }

    private async Task<int> CountTitlesForGenreAsync(string genreName, CancellationToken ct)
    {
        var stored = await catalog.Titles.AsNoTracking()
            .Where(x => x.Genre.Contains(genreName))
            .Select(x => x.Genre)
            .ToListAsync(ct);
        return stored.Count(value => ContainsGenre(value, genreName));
    }

    private static bool ContainsGenre(string? storedValue, string genreName) =>
        !string.IsNullOrWhiteSpace(storedValue)
        && storedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part => string.Equals(part, genreName, StringComparison.OrdinalIgnoreCase));

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static async Task DeleteInBatchesAsync<TEntity>(IQueryable<TEntity> source, DbContext context, CancellationToken ct) where TEntity : class
    {
        while (true)
        {
            var batch = await source.Take(DeleteBatchSize).ToListAsync(ct);
            if (batch.Count == 0) return;

            context.Set<TEntity>().RemoveRange(batch);
            await context.SaveChangesAsync(ct);
            if (batch.Count < DeleteBatchSize) return;
        }
    }
}
