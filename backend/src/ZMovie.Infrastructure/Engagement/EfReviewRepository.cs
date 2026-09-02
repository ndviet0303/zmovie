using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfReviewRepository(EngagementDbContext db) : IReviewRepository
{
    private DbSet<Review> Reviews => db.TitleReviews;

    public Task<Review?> FindByTitleAndUserAsync(TitleId titleId, UserId userId, CancellationToken ct) =>
        Reviews.SingleOrDefaultAsync(review => review.TitleId == titleId && review.UserId == userId, ct);

    public Task<Review?> FindByIdAsync(ReviewId reviewId, CancellationToken ct) =>
        Reviews.SingleOrDefaultAsync(review => review.Id == reviewId, ct);

    public void Add(Review review) => Reviews.Add(review);

    public void Remove(Review review) => Reviews.Remove(review);

    public async Task SaveChangesAsync(CancellationToken ct) =>
        await db.SaveChangesAsync(ct);
}
