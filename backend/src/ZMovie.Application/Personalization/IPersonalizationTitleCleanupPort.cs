using ZMovie.Domain.Personalization;

namespace ZMovie.Application.Personalization;

public interface IPersonalizationTitleCleanupPort
{
    Task DeleteByTitleIdAsync(TitleId titleId, CancellationToken ct);
}
