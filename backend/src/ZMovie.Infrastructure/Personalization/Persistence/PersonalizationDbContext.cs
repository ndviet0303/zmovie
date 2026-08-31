using Microsoft.EntityFrameworkCore;
using ZMovie.Domain.Personalization;

namespace ZMovie.Infrastructure.Personalization.Persistence;

public sealed class PersonalizationDbContext(DbContextOptions<PersonalizationDbContext> options) : DbContext(options)
{
    public DbSet<AssistantLearningEvent> AssistantLearningEvents => Set<AssistantLearningEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfiguration(new AssistantLearningEventConfiguration());
    }
}
