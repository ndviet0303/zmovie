using System.Data;
using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Identity.Persistence;

namespace ZMovie.Infrastructure.Identity;

public sealed class EfUserRepository(IdentityDbContext db) : IUserRepository
{
    private DbSet<User> Users => db.Users;

    public Task<User?> FindByIdAsync(UserId id, CancellationToken ct) =>
        Users.SingleOrDefaultAsync(user => user.Id == id, ct);

    public Task<User?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken ct) =>
        Users.SingleOrDefaultAsync(user => user.ExternalIdentity == externalIdentity, ct);

    public void Add(User user) => Users.Add(user);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var addedUsers = db.ChangeTracker.Entries<User>()
                .Where(e => e.State == EntityState.Added)
                .ToList();

            if (addedUsers.Count > 0)
            {
                foreach (var entry in addedUsers)
                {
                    entry.State = EntityState.Detached;
                }
            }

            throw;
        }
    }

    public async Task<SetRoleOutcome> ChangeRoleWithLastAdminGuardAsync(UserId userId, Role newRole, bool guardLastAdmin, CancellationToken ct)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var user = await Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return SetRoleOutcome.NotFound;

        if (guardLastAdmin)
        {
            var adminCount = await Users.CountAsync(x => x.Role == Role.Admin, ct);
            var decision = LastAdminPolicy.EvaluateDemotion(user.Role, newRole, adminCount);
            if (decision == LastAdminDecision.RefusedLastAdmin)
            {
                return SetRoleOutcome.LastAdmin;
            }
        }

        user.ChangeRole(newRole);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        return SetRoleOutcome.Updated;
    }
}
