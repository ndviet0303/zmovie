using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Application.Identity;
using ZMovie.Application.Personalization;
using ZMovie.Application.Search;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Api.Tests.Infrastructure;

public sealed class ZMovieWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string MemberCredential = "google-valid-member-credential";
    public const string AdminCredential = "google-valid-admin-credential";
    public static readonly Guid TitleId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid GenreId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid MemberId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    public static readonly Guid AdminId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    public static readonly Guid RecommendationId = Guid.Parse("99999999-9999-4999-8999-999999999999");

    private readonly string _databaseName = $"zmovie-contract-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ZMovie"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                ["Admin:Allowlist:0"] = "admin@zmovie.test",
                ["LocalAi:BaseUrl"] = "http://localhost:8788",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<DbContextOptions<EngagementDbContext>>();
            services.RemoveAll<DbContextOptions<AnalyticsDbContext>>();
            services.RemoveAll<DbContextOptions<PersonalizationDbContext>>();
            services.RemoveAll<CatalogDbContext>();
            services.RemoveAll<IdentityDbContext>();
            services.RemoveAll<EngagementDbContext>();
            services.RemoveAll<AnalyticsDbContext>();
            services.RemoveAll<PersonalizationDbContext>();

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(_databaseName).UseInternalServiceProvider(inMemoryProvider));
            services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(_databaseName).UseInternalServiceProvider(inMemoryProvider));
            services.AddDbContext<EngagementDbContext>(options => options.UseInMemoryDatabase(_databaseName).UseInternalServiceProvider(inMemoryProvider));
            services.AddDbContext<AnalyticsDbContext>(options => options.UseInMemoryDatabase(_databaseName).UseInternalServiceProvider(inMemoryProvider));
            services.AddDbContext<PersonalizationDbContext>(options => options.UseInMemoryDatabase(_databaseName).UseInternalServiceProvider(inMemoryProvider));

            services.RemoveAll<IGoogleIdentityVerifier>();
            services.RemoveAll<IAdminAllowlist>();
            services.RemoveAll<ISearchCatalogStore>();
            services.RemoveAll<IAssistantTextGenerator>();
            services.RemoveAll<IAssistantImpressionRecorder>();
            services.AddSingleton<IGoogleIdentityVerifier, ContractGoogleIdentityVerifier>();
            services.AddSingleton<IAdminAllowlist, ContractAllowlist>();
            services.AddSingleton<ISearchCatalogStore, ContractSearchCatalogStore>();
            services.AddSingleton<IAssistantTextGenerator, ContractAssistantTextGenerator>();
            services.AddSingleton<IAssistantImpressionRecorder, ContractAssistantImpressionRecorder>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var engagementDb = scope.ServiceProvider.GetRequiredService<EngagementDbContext>();
        var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var personalizationDb = scope.ServiceProvider.GetRequiredService<PersonalizationDbContext>();

        catalogDb.Database.EnsureCreated();
        identityDb.Database.EnsureCreated();
        engagementDb.Database.EnsureCreated();
        analyticsDb.Database.EnsureCreated();
        personalizationDb.Database.EnsureCreated();

        Seed(catalogDb, identityDb);
        return host;
    }

    public HttpClient CreateContractClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    private static void Seed(CatalogDbContext catalog, IdentityDbContext identity)
    {
        var timestamp = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);
        catalog.Titles.Add(Title.Create(
            new TitleId(TitleId),
            TitleSlug.Parse("baseline-title"),
            new LocalizedText("Phim nền", "Baseline Title"),
            new LocalizedText("Phim dùng để khóa hợp đồng HTTP.", "A baseline movie for HTTP contracts."),
            "Drama",
            ReleaseYear.FromInt(2026),
            TitleType.Movie,
            "https://example.test/poster.jpg",
            Runtime.FromMinutes(100),
            true,
            timestamp));
        catalog.Genres.Add(Genre.Create(
            new GenreId(GenreId),
            "drama",
            "Drama",
            timestamp));
        catalog.SaveChanges();

        identity.Users.Add(User.Create(
            new UserId(MemberId),
            new ExternalIdentity("member-subject"),
            "member@zmovie.test",
            "Contract Member",
            "https://example.test/member.jpg",
            Role.Member,
            timestamp));
        identity.Users.Add(User.Create(
            new UserId(AdminId),
            new ExternalIdentity("admin-subject"),
            "admin@zmovie.test",
            "Contract Admin",
            null,
            Role.Admin,
            timestamp));
        identity.SaveChanges();
    }

    private sealed class ContractGoogleIdentityVerifier : IGoogleIdentityVerifier
    {
        public Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct)
        {
            GoogleIdentity? identity = credential switch
            {
                MemberCredential => new("member-subject", "member@zmovie.test", "Contract Member", "https://example.test/member.jpg"),
                AdminCredential => new("admin-subject", "admin@zmovie.test", "Contract Admin", null),
                _ => null,
            };
            return Task.FromResult(identity);
        }
    }

    private sealed class ContractAllowlist : IAdminAllowlist
    {
        public bool IsAllowlisted(string email) => email == "admin@zmovie.test";
    }

    private sealed class ContractSearchCatalogStore : ISearchCatalogStore
    {
        public Task<TitleListResponse> SearchAsync(string query, string? type, string? genre, string locale, CancellationToken ct) =>
            Task.FromResult(new TitleListResponse(
                [new TitleSummary("baseline-title", locale == "en" ? "Baseline Title" : "Phim nền", "Drama", 2026, "movie", "https://example.test/poster.jpg")],
                1));
    }

    private sealed class ContractAssistantTextGenerator : IAssistantTextGenerator
    {
        public Task<string?> GenerateAsync(AssistantGenerationRequest request, CancellationToken ct) =>
            Task.FromResult<string?>("Contract assistant reply.");
    }

    private sealed class ContractAssistantImpressionRecorder : IAssistantImpressionRecorder
    {
        public Task<Guid?> RecordImpressionAsync(Guid userId, string message, IReadOnlyList<string> titleSlugs, CancellationToken ct) =>
            Task.FromResult<Guid?>(RecommendationId);
    }
}
