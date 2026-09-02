using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public interface IReviewRepository
{
    Task<Review?> FindByTitleAndUserAsync(TitleId titleId, UserId userId, CancellationToken ct);
    Task<Review?> FindByIdAsync(ReviewId reviewId, CancellationToken ct);
    void Add(Review review);
    void Remove(Review review);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IReviewQueries
{
    Task<IReadOnlyList<ReviewEntry>> ListByTitleAsync(TitleId titleId, CancellationToken ct);
}
