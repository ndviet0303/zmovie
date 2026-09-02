using Microsoft.EntityFrameworkCore;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;

namespace ZMovie.Api.Tests.Infrastructure;

internal sealed class TestDatabase : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString("N");

    public CatalogDbContext Db { get; }
    public CatalogDbContext Catalog => Db;
    public IdentityDbContext Identity { get; }
    public EngagementDbContext Engagement { get; }
    public AnalyticsDbContext Analytics { get; }
    public PersonalizationDbContext Personalization { get; }
    public LegacyCatalogDbContext Legacy { get; }

    public TestDatabase()
    {
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>().UseInMemoryDatabase(_dbName).Options;
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(_dbName).Options;
        var engagementOptions = new DbContextOptionsBuilder<EngagementDbContext>().UseInMemoryDatabase(_dbName).Options;
        var analyticsOptions = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(_dbName).Options;
        var personalizationOptions = new DbContextOptionsBuilder<PersonalizationDbContext>().UseInMemoryDatabase(_dbName).Options;
        var legacyOptions = new DbContextOptionsBuilder<LegacyCatalogDbContext>().UseInMemoryDatabase(_dbName).Options;

        Db = new CatalogDbContext(catalogOptions);
        Identity = new IdentityDbContext(identityOptions);
        Engagement = new EngagementDbContext(engagementOptions);
        Analytics = new AnalyticsDbContext(analyticsOptions);
        Personalization = new PersonalizationDbContext(personalizationOptions);
        Legacy = new LegacyCatalogDbContext(legacyOptions);

        Db.Database.EnsureCreated();
        Identity.Database.EnsureCreated();
        Engagement.Database.EnsureCreated();
        Analytics.Database.EnsureCreated();
        Personalization.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        Identity.Dispose();
        Engagement.Dispose();
        Analytics.Dispose();
        Personalization.Dispose();
        Legacy.Dispose();
    }
}
