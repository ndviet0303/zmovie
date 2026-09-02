using ZMovie.Domain.Catalog;

namespace ZMovie.Application.Catalog;

public interface ICatalogTitleCleanupPort
{
    Task<Title?> FindBySlugAsync(string slug, CancellationToken ct);
    Task DeleteTitleAggregateAsync(TitleId titleId, CancellationToken ct);
}
