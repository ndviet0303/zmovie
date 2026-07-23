using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using ZMovie.Application.Engagement;

namespace ZMovie.Infrastructure.Engagement;

/// <summary>Protects the event table from read bursts while keeping rankings fresh enough for discovery.</summary>
public sealed class CachedViewAnalyticsStore(EfUserLibraryStore inner, IMemoryCache cache) : IViewAnalyticsStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    public Task<ViewRecordedResponse> RecordAsync(Guid titleId, Guid? userId, string sessionId, int? episodeNumber, CancellationToken ct) =>
        inner.RecordAsync(titleId, userId, sessionId, episodeNumber, ct);

    public Task<long> GetViewCountAsync(Guid titleId, CancellationToken ct) => inner.GetViewCountAsync(titleId, ct);

    public async Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, CancellationToken ct)
    {
        var key = $"analytics:top:{period}:{limit}";
        if (cache.TryGetValue<IReadOnlyList<TopViewCount>>(key, out var cached) && cached is not null) return cached;

        var gate = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<IReadOnlyList<TopViewCount>>(key, out cached) && cached is not null) return cached;
            cached = await inner.GetTopAsync(period, limit, ct);
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
