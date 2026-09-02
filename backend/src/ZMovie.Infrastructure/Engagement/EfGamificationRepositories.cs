using System.Collections.Concurrent;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Engagement;

namespace ZMovie.Infrastructure.Engagement;

public sealed class InMemoryShortClipRepository : IShortClipRepository
{
    private readonly ConcurrentBag<ShortClip> _clips = [];

    public Task<IReadOnlyList<ShortClip>> GetFeedAsync(int limit, CancellationToken ct)
    {
        IReadOnlyList<ShortClip> result = _clips.Take(limit).ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(ShortClip clip, CancellationToken ct)
    {
        _clips.Add(clip);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryUserExpRepository : IUserExpRepository
{
    private readonly ConcurrentDictionary<Guid, int> _userExp = new();

    public Task<int> GetTotalExpAsync(Guid userId, CancellationToken ct)
    {
        _userExp.TryGetValue(userId, out var total);
        return Task.FromResult(total);
    }

    public Task AddAsync(UserExpLedger ledger, CancellationToken ct)
    {
        _userExp.AddOrUpdate(ledger.UserId, ledger.ExpGained, (_, existing) => existing + ledger.ExpGained);
        return Task.CompletedTask;
    }
}
