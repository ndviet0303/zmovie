using Microsoft.AspNetCore.SignalR;
using ZMovie.Application.WatchParty;

namespace ZMovie.Infrastructure.WatchParty;


public sealed class WatchPartyHub(IWatchPartyRegistry registry) : Hub
{

    public async Task JoinRoom(string roomId, string username, string movieSlug, int episodeNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        var result = registry.Join(
            roomId.Trim(),
            Context.ConnectionId,
            username.Trim(),
            movieSlug.Trim(),
            Math.Max(1, episodeNumber));

        await Clients.Caller.SendAsync("RoomJoined", new
        {
            result.Room.RoomId,
            result.Room.MovieSlug,
            result.Room.EpisodeNumber,
            result.Room.IsPlaying,
            result.Room.CurrentTime,
            result.ActiveUsers
        });
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", new
        {
            Username = username,
            ActiveCount = result.Room.ActiveUserCount
        });
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        if (registry.Leave(roomId, Context.ConnectionId) is { } result)
        {
            await Clients.Group(roomId).SendAsync("UserLeft", new
            {
                result.Username,
                ActiveCount = result.ActiveUserCount
            });
        }
    }

    public async Task SyncPlay(string roomId, double currentTime)
    {
        registry.UpdatePlayback(roomId, true, currentTime);
        await Clients.OthersInGroup(roomId).SendAsync("PlaybackSynced", new
        {
            Action = "play",
            CurrentTime = currentTime,
            SenderId = Context.ConnectionId
        });
    }

    public async Task SyncPause(string roomId, double currentTime)
    {
        registry.UpdatePlayback(roomId, false, currentTime);
        await Clients.OthersInGroup(roomId).SendAsync("PlaybackSynced", new
        {
            Action = "pause",
            CurrentTime = currentTime,
            SenderId = Context.ConnectionId
        });
    }

    public async Task SyncSeek(string roomId, double currentTime)
    {
        registry.UpdatePlayback(roomId, null, currentTime);
        await Clients.OthersInGroup(roomId).SendAsync("PlaybackSynced", new
        {
            Action = "seek",
            CurrentTime = currentTime,
            SenderId = Context.ConnectionId
        });
    }

    public async Task SendPartyChat(string roomId, string username, string message)
    {
        await Clients.Group(roomId).SendAsync("ChatMessageReceived", new
        {
            Username = username,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task SendDanmaku(string roomId, string episodeId, double timeSeconds, string content, string color)
    {
        await Clients.Group(roomId).SendAsync("DanmakuReceived", new
        {
            EpisodeId = episodeId,
            TimeSeconds = timeSeconds,
            Content = content,
            Color = string.IsNullOrWhiteSpace(color) ? "#ffffff" : color,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var result in registry.LeaveAll(Context.ConnectionId))
        {
            await Clients.Group(result.RoomId).SendAsync("UserLeft", new
            {
                result.Username,
                ActiveCount = result.ActiveUserCount
            });
        }
        await base.OnDisconnectedAsync(exception);
    }
}
