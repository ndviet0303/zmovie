using System.Collections.Concurrent;
using System.Security.Cryptography;
using ZMovie.Application.WatchParty;

namespace ZMovie.Infrastructure.WatchParty;

public sealed class InMemoryWatchPartyRegistry(TimeProvider timeProvider) : IWatchPartyRegistry
{
    private readonly ConcurrentDictionary<string, RoomEntry> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public CreatedWatchPartyRoom Create(string movieSlug, int episodeNumber)
    {
        while (true)
        {
            var roomId = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var managementToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            var now = timeProvider.GetUtcNow();
            var entry = new RoomEntry(movieSlug, episodeNumber, managementToken, now);
            if (_rooms.TryAdd(roomId, entry))
                return new CreatedWatchPartyRoom(Snapshot(roomId, entry), managementToken);
        }
    }

    public WatchPartyJoinResult Join(string roomId, string connectionId, string username, string movieSlug, int episodeNumber)
    {
        var now = timeProvider.GetUtcNow();
        var entry = _rooms.GetOrAdd(roomId, _ => new RoomEntry(movieSlug, episodeNumber, null, now));
        lock (entry.SyncRoot)
        {
            entry.Users[connectionId] = username;
            entry.LastUpdated = now;
            return new WatchPartyJoinResult(
                SnapshotUnsafe(roomId, entry),
                entry.Users.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
    }

    public WatchPartyLeaveResult? Leave(string roomId, string connectionId)
    {
        if (!_rooms.TryGetValue(roomId, out var entry)) return null;
        lock (entry.SyncRoot)
        {
            if (!entry.Users.Remove(connectionId, out var username)) return null;
            entry.LastUpdated = timeProvider.GetUtcNow();
            return new WatchPartyLeaveResult(roomId, username, entry.Users.Count);
        }
    }

    public IReadOnlyList<WatchPartyLeaveResult> LeaveAll(string connectionId)
    {
        var results = new List<WatchPartyLeaveResult>();
        foreach (var roomId in _rooms.Keys)
        {
            if (Leave(roomId, connectionId) is { } result) results.Add(result);
        }
        return results;
    }

    public IReadOnlyList<WatchPartyRoom> List() => _rooms
        .Select(pair => Snapshot(pair.Key, pair.Value))
        .OrderByDescending(room => room.ActiveUserCount)
        .ThenByDescending(room => room.LastUpdated)
        .ToList();

    public void UpdatePlayback(string roomId, bool? isPlaying, double currentTime)
    {
        if (!_rooms.TryGetValue(roomId, out var entry)) return;
        lock (entry.SyncRoot)
        {
            if (isPlaying.HasValue) entry.IsPlaying = isPlaying.Value;
            entry.CurrentTime = Math.Max(0, currentTime);
            entry.LastUpdated = timeProvider.GetUtcNow();
        }
    }

    public bool Delete(string roomId, string managementToken)
    {
        if (!_rooms.TryGetValue(roomId, out var entry) || string.IsNullOrEmpty(entry.ManagementToken)) return false;
        var expected = System.Text.Encoding.UTF8.GetBytes(entry.ManagementToken);
        var actual = System.Text.Encoding.UTF8.GetBytes(managementToken);
        if (expected.Length != actual.Length || !CryptographicOperations.FixedTimeEquals(expected, actual)) return false;
        return _rooms.TryRemove(new KeyValuePair<string, RoomEntry>(roomId, entry));
    }

    private static WatchPartyRoom Snapshot(string roomId, RoomEntry entry)
    {
        lock (entry.SyncRoot) return SnapshotUnsafe(roomId, entry);
    }

    private static WatchPartyRoom SnapshotUnsafe(string roomId, RoomEntry entry) => new(
        roomId,
        entry.MovieSlug,
        entry.EpisodeNumber,
        entry.Users.Count,
        entry.IsPlaying,
        entry.CurrentTime,
        entry.CreatedAt,
        entry.LastUpdated);

    private sealed class RoomEntry(string movieSlug, int episodeNumber, string? managementToken, DateTimeOffset createdAt)
    {
        public object SyncRoot { get; } = new();
        public string MovieSlug { get; } = movieSlug;
        public int EpisodeNumber { get; } = episodeNumber;
        public string? ManagementToken { get; } = managementToken;
        public Dictionary<string, string> Users { get; } = new(StringComparer.Ordinal);
        public bool IsPlaying { get; set; }
        public double CurrentTime { get; set; }
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public DateTimeOffset LastUpdated { get; set; } = createdAt;
    }
}
