using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public interface IEngagementTitleCleanupPort
{
    Task DeleteByTitleIdAsync(TitleId titleId, CancellationToken ct);
}
