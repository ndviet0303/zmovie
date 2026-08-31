using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;

namespace ZMovie.Infrastructure.Engagement;

public sealed class EfReviewQueries(EngagementDbContext db) : IReviewQueries
{
    public async Task<IReadOnlyList<ReviewEntry>> ListByTitleAsync(TitleId titleId, CancellationToken ct)
    {
        var rows = await db.TitleReviews
            .AsNoTracking()
            .Where(review => review.TitleId == titleId)
            .OrderByDescending(review => review.UpdatedAt)
            .Select(review => new
            {
                review.Id,
                review.AuthorName,
                review.Rating,
                review.Comment,
                review.UpdatedAt,
            })
            .ToListAsync(ct);

        return rows.Select(row => new ReviewEntry(
            row.Id.Value,
            row.AuthorName,
            row.Rating.Value,
            row.Comment,
            row.UpdatedAt)).ToList();
    }
}
