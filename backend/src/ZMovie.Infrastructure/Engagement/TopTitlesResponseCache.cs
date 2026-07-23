using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using ZMovie.Application.Engagement;

namespace ZMovie.Infrastructure.Engagement;

public sealed class TopTitlesResponseCache(IMemoryCache cache) : ITopTitlesResponseCache
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public async Task<IReadOnlyList<TopTitleResponse>> GetOrCreateAsync(TopPeriod period, string locale, int limit, Func<CancellationToken, Task<IReadOnlyList<TopTitleResponse>>> factory, CancellationToken ct)
    {
        var key = $"analytics:top-response:{period}:{locale}:{limit}";
        if (cache.TryGetValue<IReadOnlyList<TopTitleResponse>>(key, out var cached) && cached is not null) return cached;

        var gate = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<IReadOnlyList<TopTitleResponse>>(key, out cached) && cached is not null) return cached;
            cached = await factory(ct);
            cache.Set(key, cached, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Expiration(period), Size = 1 });
            return cached;
        }
        finally
        {
            gate.Release();
        }
    }

    private static TimeSpan Expiration(TopPeriod period) => period switch
    {
        TopPeriod.Day => TimeSpan.FromMinutes(1),
        TopPeriod.Week => TimeSpan.FromMinutes(5),
        TopPeriod.Month => TimeSpan.FromMinutes(15),
        _ => TimeSpan.FromMinutes(1),
    };
}
