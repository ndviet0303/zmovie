using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfEpisodeRepository(CatalogDbContext db) : IEpisodeRepository
{
    private DbSet<Episode> Episodes => db.Set<Episode>();

    public async Task<IReadOnlyList<Episode>> ListByTitleIdAsync(TitleId titleId, CancellationToken ct) =>
        await Episodes.Where(x => x.TitleId == titleId).OrderBy(x => x.Number).ToListAsync(ct);

    public Task<Episode?> FindByNumberAsync(TitleId titleId, int number, CancellationToken ct) =>
        Episodes.FirstOrDefaultAsync(x => x.TitleId == titleId && x.Number == number, ct);

    public void Add(Episode episode) => Episodes.Add(episode);

    public void Remove(Episode episode) => Episodes.Remove(episode);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task RemoveByTitleIdAsync(TitleId titleId, CancellationToken ct)
    {
        var episodes = await Episodes.Where(x => x.TitleId == titleId).ToListAsync(ct);
        if (episodes.Count > 0)
        {
            Episodes.RemoveRange(episodes);
            await db.SaveChangesAsync(ct);
        }
    }
}
