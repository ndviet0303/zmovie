using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfSavedTitleRepository(EngagementDbContext db) : ISavedTitleRepository
{
    private DbSet<SavedTitle> SavedTitles => db.SavedTitles;

    public Task<SavedTitle?> FindAsync(UserId userId, TitleId titleId, CancellationToken ct) =>
        SavedTitles.SingleOrDefaultAsync(saved => saved.UserId == userId && saved.TitleId == titleId, ct);

    public void Add(SavedTitle savedTitle) => SavedTitles.Add(savedTitle);

    public void Remove(SavedTitle savedTitle) => SavedTitles.Remove(savedTitle);

    public async Task SaveChangesAsync(CancellationToken ct) =>
        await db.SaveChangesAsync(ct);
}
