namespace ZMovie.Application.WatchParty;

public sealed record WatchPartyRoom(
    string RoomId,
    string MovieSlug,
    int EpisodeNumber,
    int ActiveUserCount,
    bool IsPlaying,
    double CurrentTime,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdated);

public sealed record CreatedWatchPartyRoom(WatchPartyRoom Room, string ManagementToken);
public sealed record WatchPartyJoinResult(WatchPartyRoom Room, IReadOnlyList<string> ActiveUsers);
public sealed record WatchPartyLeaveResult(string RoomId, string Username, int ActiveUserCount);

public interface IWatchPartyRegistry
{
    CreatedWatchPartyRoom Create(string movieSlug, int episodeNumber);
    WatchPartyJoinResult Join(string roomId, string connectionId, string username, string movieSlug, int episodeNumber);
    WatchPartyLeaveResult? Leave(string roomId, string connectionId);
    IReadOnlyList<WatchPartyLeaveResult> LeaveAll(string connectionId);
    IReadOnlyList<WatchPartyRoom> List();
    void UpdatePlayback(string roomId, bool? isPlaying, double currentTime);
    bool Delete(string roomId, string managementToken);
}
