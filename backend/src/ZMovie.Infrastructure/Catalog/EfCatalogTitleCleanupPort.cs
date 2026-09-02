using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;

namespace ZMovie.Infrastructure.Catalog;

public sealed class EfCatalogTitleCleanupPort(CatalogDbContext db) : ICatalogTitleCleanupPort
{
    private const int DeleteBatchSize = 500;

    public async Task<Title?> FindBySlugAsync(string slug, CancellationToken ct)
    {
        if (!TitleSlug.TryCreate(slug, out var titleSlug)) return null;
        return await db.Titles.FirstOrDefaultAsync(x => x.Slug == titleSlug, ct);
    }

    public async Task DeleteTitleAggregateAsync(TitleId titleId, CancellationToken ct)
    {
        await DeleteInBatchesAsync(db.TitleGenres.Where(x => x.TitleId == titleId), db, ct);

        var episodeIds = await db.Episodes.Where(x => x.TitleId == titleId).Select(x => x.Id).ToListAsync(ct);
        if (episodeIds.Count > 0)
        {
            await DeleteInBatchesAsync(db.EpisodeStreamSources.Where(x => episodeIds.Contains(x.EpisodeId)), db, ct);
        }

        await DeleteInBatchesAsync(db.Episodes.Where(x => x.TitleId == titleId), db, ct);

        var title = await db.Titles.FirstOrDefaultAsync(x => x.Id == titleId, ct);
        if (title is not null)
        {
            db.Titles.Remove(title);
            await db.SaveChangesAsync(ct);
        }
    }


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
