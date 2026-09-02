using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfTitleRepository(CatalogDbContext db) : ITitleRepository
{
    private DbSet<Title> Titles => db.Set<Title>();
    private DbSet<TitleGenreAssignment> TitleGenres => db.Set<TitleGenreAssignment>();

    public Task<Title?> FindByIdAsync(TitleId id, CancellationToken ct) =>
        Titles.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Title?> FindBySlugAsync(TitleSlug slug, CancellationToken ct) =>
        Titles.FirstOrDefaultAsync(x => x.Slug == slug, ct);

    public void Add(Title title) => Titles.Add(title);

    public void Remove(Title title) => Titles.Remove(title);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task SyncGenreAssignmentsAsync(TitleId titleId, IReadOnlyList<GenreId> genreIds, DateTimeOffset occurredAt, CancellationToken ct)
    {
        var existing = await TitleGenres.Where(x => x.TitleId == titleId).ToListAsync(ct);
        var targetSet = genreIds.ToHashSet();

        var toRemove = existing.Where(x => !targetSet.Contains(x.GenreId)).ToList();
        if (toRemove.Count > 0)
        {
            TitleGenres.RemoveRange(toRemove);
        }

        var existingSet = existing.Select(x => x.GenreId).ToHashSet();
        var toAdd = genreIds.Where(id => !existingSet.Contains(id))
            .Select(id => TitleGenreAssignment.Create(titleId, id, occurredAt))
            .ToList();

        if (toAdd.Count > 0)
        {
            TitleGenres.AddRange(toAdd);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<GenreId>> GetAssignedGenreIdsAsync(TitleId titleId, CancellationToken ct) =>
        await TitleGenres.AsNoTracking()
            .Where(x => x.TitleId == titleId)
            .Select(x => x.GenreId)
            .ToListAsync(ct);
}
