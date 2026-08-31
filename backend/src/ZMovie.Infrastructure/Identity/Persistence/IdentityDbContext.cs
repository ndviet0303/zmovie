using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
