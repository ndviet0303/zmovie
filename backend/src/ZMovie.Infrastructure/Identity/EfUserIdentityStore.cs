using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Identity;

public sealed class EfUserIdentityStore(CatalogDbContext db) : IUserIdentityStore
{
    public async Task<AuthenticatedUser> UpsertGoogleUserAsync(GoogleIdentity identity, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.GoogleSubject == identity.Subject, ct);
        if (user is null)
        {
            user = new ZMovieUser { GoogleSubject = identity.Subject, Email = identity.Email, DisplayName = identity.DisplayName, AvatarUrl = identity.AvatarUrl };
            db.Users.Add(user);
        }
        else
        {
            user.Email = identity.Email;
            user.DisplayName = identity.DisplayName;
            user.AvatarUrl = identity.AvatarUrl;
            user.LastSignedInAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return new(user.Id, user.Email, user.DisplayName, user.AvatarUrl);
    }
}
