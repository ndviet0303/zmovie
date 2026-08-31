using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Identity.Persistence;

namespace ZMovie.Infrastructure.Identity;

public sealed class EfUserQueries(IdentityDbContext db) : IUserQueries
{
    private DbSet<User> Users => db.Users;

    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(string? query, string? role, int page, int pageSize, CancellationToken ct)
    {
        var users = Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.ToLowerInvariant();
            users = users.Where(x => x.Email.ToLower().Contains(term) || x.DisplayName.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(role) && Role.TryCreate(role, out var parsedRole))
        {
            users = users.Where(x => x.Role == parsedRole);
        }

        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        return await users
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(normalizedPageSize * (normalizedPage - 1))
            .Take(normalizedPageSize)
            .Select(x => new UserSummary(x.Id.Value, x.Email, x.DisplayName, x.AvatarUrl, x.Role.Value, x.CreatedAt, x.LastSignedInAt))
            .ToListAsync(ct);
    }

    public async Task<UserSummary?> GetUserAsync(UserId userId, CancellationToken ct) =>
        await Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserSummary(x.Id.Value, x.Email, x.DisplayName, x.AvatarUrl, x.Role.Value, x.CreatedAt, x.LastSignedInAt))
            .FirstOrDefaultAsync(ct);

    public Task<int> CountAdminsAsync(CancellationToken ct) =>
        Users.AsNoTracking().CountAsync(x => x.Role == Role.Admin, ct);
}
