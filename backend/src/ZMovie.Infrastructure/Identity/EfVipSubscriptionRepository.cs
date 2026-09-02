using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Identity.Persistence;

namespace ZMovie.Infrastructure.Identity;

public sealed class EfVipSubscriptionRepository(IdentityDbContext db) : IVipSubscriptionRepository
{
    public async Task<VipSubscription?> FindByGatewayReferenceAsync(string gateway, string reference, CancellationToken ct)
    {
        return await db.VipSubscriptions
            .FirstOrDefaultAsync(x => x.PaymentGateway == gateway && x.TransactionReference == reference, ct);
    }

    public async Task AddAsync(VipSubscription subscription, CancellationToken ct)
    {
        await db.VipSubscriptions.AddAsync(subscription, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<VipSubscription>> ListByUserIdAsync(UserId userId, CancellationToken ct)
    {
        return await db.VipSubscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.PaidAt)
            .ToListAsync(ct);
    }
}
