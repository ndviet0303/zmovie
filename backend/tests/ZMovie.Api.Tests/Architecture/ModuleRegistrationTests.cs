using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZMovie.Application;
using ZMovie.Application.Administration;
using ZMovie.Application.Analytics;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Application.Identity;
using ZMovie.Application.Personalization;
using ZMovie.Application.Search;
using ZMovie.Infrastructure;
using ZMovie.Infrastructure.Analytics;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;
using Xunit;

namespace ZMovie.Api.Tests.Architecture;

public sealed class ModuleRegistrationTests
{
    [Fact]
    public void Application_and_infrastructure_registrations_preserve_service_lifetimes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ZMovie"] = "Host=unused;Database=unused;Username=unused;Password=unused",
                ["LocalAi:BaseUrl"] = "http://localhost:8788",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddZMovieApplication();
        services.AddZMovieInfrastructure(configuration);

        AssertLifetime<TimeProvider>(services, ServiceLifetime.Singleton);
        AssertLifetime<DbContextOptions<CatalogDbContext>>(services, ServiceLifetime.Scoped);
        AssertLifetime<DbContextOptions<IdentityDbContext>>(services, ServiceLifetime.Scoped);
        AssertLifetime<DbContextOptions<EngagementDbContext>>(services, ServiceLifetime.Scoped);
        AssertLifetime<DbContextOptions<AnalyticsDbContext>>(services, ServiceLifetime.Scoped);
        AssertLifetime<DbContextOptions<PersonalizationDbContext>>(services, ServiceLifetime.Scoped);
        AssertLifetime<ICatalogReadStore>(services, ServiceLifetime.Scoped);
        AssertLifetime<ITitleRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IEpisodeRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IGenreRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<ISearchCatalogStore>(services, ServiceLifetime.Transient);
        AssertLifetime<IAdminAllowlist>(services, ServiceLifetime.Singleton);
        AssertLifetime<IUserRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IUserQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<IGoogleIdentityVerifier>(services, ServiceLifetime.Scoped);
        AssertLifetime<IViewEventRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<EfViewAnalyticsQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<IViewAnalyticsQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<ISavedTitleRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IWatchProgressRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IUserLibraryQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<IReviewRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IReviewQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<ITopTitlesResponseCache>(services, ServiceLifetime.Singleton);
        AssertLifetime<IRecommendationEngine>(services, ServiceLifetime.Singleton);
        AssertLifetime<ILibraryCatalogReader>(services, ServiceLifetime.Scoped);
        AssertLifetime<IPersonalizationLearningRepository>(services, ServiceLifetime.Scoped);
        AssertLifetime<IPersonalizationQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<IAssistantImpressionRecorder>(services, ServiceLifetime.Scoped);
        AssertLifetime<ICatalogAssistantStore>(services, ServiceLifetime.Scoped);
        AssertLifetime<ITransactionCoordinator>(services, ServiceLifetime.Scoped);
        AssertLifetime<IAdminDashboardQueries>(services, ServiceLifetime.Scoped);
        AssertLifetime<IAdminTitleDeletionCoordinator>(services, ServiceLifetime.Scoped);
        AssertLifetime<ICatalogAdministrationService>(services, ServiceLifetime.Scoped);
        AssertLifetime<IAssistantTextGenerator>(services, ServiceLifetime.Transient);

        using var provider = services.BuildServiceProvider();
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    private static void AssertLifetime<TService>(IServiceCollection services, ServiceLifetime expected)
    {
        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(TService));
        Assert.Equal(expected, descriptor.Lifetime);
    }
}
