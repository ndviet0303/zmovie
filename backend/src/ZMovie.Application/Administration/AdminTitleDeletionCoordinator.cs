using ZMovie.Application.Analytics;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Application.Personalization;
using AnalyticsTitleId = ZMovie.Domain.Analytics.TitleId;
using EngagementTitleId = ZMovie.Domain.Engagement.TitleId;
using PersonalizationTitleId = ZMovie.Domain.Personalization.TitleId;

namespace ZMovie.Application.Administration;

public sealed class AdminTitleDeletionCoordinator(
    ICatalogTitleCleanupPort catalogCleanup,
    IEngagementTitleCleanupPort engagementCleanup,
    IAnalyticsTitleCleanupPort analyticsCleanup,
    IPersonalizationTitleCleanupPort personalizationCleanup,
    ITransactionCoordinator transactionCoordinator) : IAdminTitleDeletionCoordinator
{
    public async Task<bool> DeleteTitleAsync(string slug, CancellationToken ct)
    {
        var title = await catalogCleanup.FindBySlugAsync(slug, ct);
        if (title is null)
        {
            return false;
        }

        var titleGuid = title.Id.Value;
        var engagementTitleId = new EngagementTitleId(titleGuid);
        var analyticsTitleId = new AnalyticsTitleId(titleGuid);
        var personalizationTitleId = new PersonalizationTitleId(titleGuid);
        var catalogTitleId = title.Id;

        await transactionCoordinator.ExecuteInTransactionAsync(async transactionCt =>
        {
            // Documented order: Engagement -> Analytics -> Personalization -> Catalog
            await engagementCleanup.DeleteByTitleIdAsync(engagementTitleId, transactionCt);
            await analyticsCleanup.DeleteByTitleIdAsync(analyticsTitleId, transactionCt);
            await personalizationCleanup.DeleteByTitleIdAsync(personalizationTitleId, transactionCt);
            await catalogCleanup.DeleteTitleAggregateAsync(catalogTitleId, transactionCt);
        }, ct);

        return true;
    }
}
