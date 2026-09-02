using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfGenreRepository(CatalogDbContext db) : IGenreRepository
{
    private DbSet<Genre> Genres => db.Set<Genre>();

    public Task<Genre?> FindByIdAsync(GenreId id, CancellationToken ct) =>
        Genres.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Genre?> FindBySlugAsync(string slug, CancellationToken ct) =>
        Genres.FirstOrDefaultAsync(x => x.Slug == slug.Trim().ToLowerInvariant(), ct);

    public Task<Genre?> FindByNameAsync(string name, CancellationToken ct) =>
        Genres.FirstOrDefaultAsync(x => EF.Functions.ILike(x.Name, name.Trim()), ct);

    public async Task<IReadOnlyList<Genre>> ListAllAsync(CancellationToken ct) =>
        await Genres.OrderBy(x => x.Name).ToListAsync(ct);

    public void Add(Genre genre) => Genres.Add(genre);

    public void Remove(Genre genre) => Genres.Remove(genre);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
