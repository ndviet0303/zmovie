using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZMovie.Application.Administration;
using ZMovie.Application.Analytics;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Application.Identity;
using ZMovie.Application.Personalization;
using ZMovie.Application.Search;
using ZMovie.Infrastructure.Administration;
using ZMovie.Infrastructure.Analytics;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Assistant;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Common;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization;
using ZMovie.Infrastructure.Personalization.Persistence;
using ZMovie.Infrastructure.Recommendations;
using ZMovie.Infrastructure.Search;

namespace ZMovie.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddZMovieInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache(options => options.SizeLimit = 10_000);
        services.AddHttpClient();

        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<PublishDomainEventsInterceptor>();

        var connectionString = configuration.GetConnectionString("ZMovie")
            ?? throw new InvalidOperationException("ConnectionStrings:ZMovie must be configured.");

        services.AddCatalogModule(connectionString);
        services.AddIdentityModule(connectionString);
        services.AddEngagementModule(connectionString);
        services.AddAnalyticsModule(connectionString);
        services.AddPersonalizationModule(connectionString);
        services.AddAdministrationModule(configuration);
        services.AddAssistantModule(configuration);
        services.AddSearchModule();
        services.AddStorageModule(configuration);

        return services;
    }

    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CatalogDbContext>((sp, options) => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_catalog", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<ICatalogReadStore, EfCatalogReadStore>();
        services.AddScoped<ITitleRepository, EfTitleRepository>();
        services.AddScoped<IEpisodeRepository, EfEpisodeRepository>();
        services.AddScoped<IGenreRepository, EfGenreRepository>();
        services.AddScoped<ICatalogTitleCleanupPort, EfCatalogTitleCleanupPort>();
        services.AddScoped<ICatalogAdministrationService, EfCatalogAdministrationService>();
        services.AddScoped<ILibraryCatalogReader, CatalogLibraryReader>();
        services.AddScoped<ICatalogAssistantStore, CatalogAssistantStore>();

        return services;
    }

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentityDbContext>((sp, options) => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_identity", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IUserQueries, EfUserQueries>();
        services.AddScoped<IGoogleIdentityVerifier, GoogleIdentityVerifier>();

        return services;
    }

    public static IServiceCollection AddEngagementModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EngagementDbContext>((sp, options) => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_engagement", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<ISavedTitleRepository, EfSavedTitleRepository>();
        services.AddScoped<IWatchProgressRepository, EfWatchProgressRepository>();
        services.AddScoped<IUserLibraryQueries, EfUserLibraryQueries>();
        services.AddScoped<IReviewRepository, EfReviewRepository>();
        services.AddScoped<IReviewQueries, EfReviewQueries>();
        services.AddScoped<IEngagementTitleCleanupPort, EfEngagementTitleCleanupPort>();
        services.AddSingleton<ITopTitlesResponseCache, TopTitlesResponseCache>();
        services.AddSingleton<IRecommendationEngine, TinyContentRecommendationEngine>();

        return services;
    }

    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AnalyticsDbContext>((sp, options) => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_analytics", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IViewEventRepository, EfViewEventRepository>();
        services.AddScoped<EfViewAnalyticsQueries>();
        services.AddScoped<IViewAnalyticsQueries, CachedViewAnalyticsQueries>(sp =>
            new CachedViewAnalyticsQueries(sp.GetRequiredService<EfViewAnalyticsQueries>(), sp.GetRequiredService<IMemoryCache>()));
        services.AddScoped<IAnalyticsTitleCleanupPort, EfAnalyticsTitleCleanupPort>();

        return services;
    }

    public static IServiceCollection AddPersonalizationModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PersonalizationDbContext>((sp, options) => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history_personalization", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IPersonalizationLearningRepository, EfPersonalizationLearningRepository>();
        services.AddScoped<IPersonalizationQueries, EfPersonalizationQueries>();
        services.AddScoped<IPersonalizationTitleCleanupPort, EfPersonalizationTitleCleanupPort>();
        services.AddScoped<IAssistantImpressionRecorder, AssistantImpressionRecorder>();

        return services;
    }

    public static IServiceCollection AddAdministrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminOptions>(configuration.GetSection("Admin"));
        services.AddSingleton<IAdminAllowlist>(sp => sp.GetRequiredService<IOptions<AdminOptions>>().Value);
        services.AddScoped<ITransactionCoordinator, NpgsqlTransactionCoordinator>();
        services.AddScoped<IAdminTitleDeletionCoordinator, AdminTitleDeletionCoordinator>();
        services.AddScoped<EfAdminDashboardQueries>();
        services.AddScoped<IAdminDashboardQueries>(sp => sp.GetRequiredService<EfAdminDashboardQueries>());

        return services;
    }

    public static IServiceCollection AddAssistantModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalAiOptions>(configuration.GetSection("LocalAi"));
        services.AddHttpClient<IAssistantTextGenerator, LocalAiAssistantTextGenerator>((serviceProvider, http) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LocalAiOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 60));
        });

        return services;
    }

    public static IServiceCollection AddSearchModule(this IServiceCollection services)
    {
        services.AddHttpClient<ISearchCatalogStore, SearchCatalogStore>();
        return services;
    }

    public static IServiceCollection AddStorageModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZMovie.Infrastructure.Storage.CloudflareR2Options>(configuration.GetSection(ZMovie.Infrastructure.Storage.CloudflareR2Options.SectionName));
        services.AddSingleton<ZMovie.Infrastructure.Storage.ICloudflareR2Storage, ZMovie.Infrastructure.Storage.CloudflareR2Storage>();
        return services;
    }
}
