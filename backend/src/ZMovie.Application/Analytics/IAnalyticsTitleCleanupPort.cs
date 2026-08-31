using ZMovie.Domain.Analytics;

namespace ZMovie.Application.Analytics;

public interface IAnalyticsTitleCleanupPort
{
    Task DeleteByTitleIdAsync(TitleId titleId, CancellationToken ct);
}
