using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Identity;

public sealed class EfUserIdentityStore(CatalogDbContext db, IOptions<AdminOptions> adminOptions) : IUserIdentityStore
{
    public async Task<AuthenticatedUser> UpsertGoogleUserAsync(GoogleIdentity identity, CancellationToken ct)
    {
        var isAllowlistedAdmin = adminOptions.Value.IsAllowlisted(identity.Email);
        var user = await db.Users.FirstOrDefaultAsync(x => x.GoogleSubject == identity.Subject, ct);
        if (user is null)
        {
            user = new ZMovieUser
            {
                GoogleSubject = identity.Subject,
                Email = identity.Email,
                DisplayName = identity.DisplayName,
                AvatarUrl = identity.AvatarUrl,
                Role = isAllowlistedAdmin ? ZMovieRoles.Admin : ZMovieRoles.Member,
            };
            db.Users.Add(user);
        }
        else
        {
            user.Email = identity.Email;
            user.DisplayName = identity.DisplayName;
            user.AvatarUrl = identity.AvatarUrl;
            user.LastSignedInAt = DateTimeOffset.UtcNow;
            // The allowlist only ever promotes. A role granted through the admin UI must not be
            // reset on the next sign-in, and dropping an entry from config must not silently demote.
            if (isAllowlistedAdmin) user.Role = ZMovieRoles.Admin;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (db.Entry(user).State == EntityState.Added)
        {
            // Two concurrent first sign-ins for the same Google subject race on ix_users_google_subject.
            db.Entry(user).State = EntityState.Detached;
            user = await db.Users.FirstAsync(x => x.GoogleSubject == identity.Subject, ct);
        }

        return new(user.Id, user.Email, user.DisplayName, user.AvatarUrl, ZMovieRoles.Normalize(user.Role));
    }
}
