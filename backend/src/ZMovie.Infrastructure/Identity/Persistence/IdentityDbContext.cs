using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<VipSubscription> VipSubscriptions => Set<VipSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new VipSubscriptionConfiguration());
    }
}

