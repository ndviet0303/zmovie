using Microsoft.EntityFrameworkCore;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Api.Tests.Infrastructure;

internal sealed class TestDatabase : IDisposable
{
    public CatalogDbContext Db { get; }

    public TestDatabase()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        Db = new CatalogDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}
