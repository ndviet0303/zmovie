using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ZMovie.Infrastructure.Realtime;

public sealed record RoomUserState(string ConnectionId, string Username);

public sealed record RoomPlaybackState
{
    public string MovieSlug { get; set; } = string.Empty;
    public int EpisodeNumber { get; set; } = 1;
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WatchPartyHub : Hub
{
    private static readonly ConcurrentDictionary<string, RoomPlaybackState> Rooms = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RoomUserState>> RoomUsers = new();

    public async Task JoinRoom(string roomId, string username, string movieSlug, int episodeNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var roomState = Rooms.GetOrAdd(roomId, _ => new RoomPlaybackState
        {
            MovieSlug = movieSlug,
            EpisodeNumber = episodeNumber,
            IsPlaying = false,
            CurrentTime = 0,
            LastUpdated = DateTimeOffset.UtcNow
        });

        var users = RoomUsers.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, RoomUserState>());
        users[Context.ConnectionId] = new RoomUserState(Context.ConnectionId, username);

        // Notify joining user of current room state
        await Clients.Caller.SendAsync("RoomJoined", new
        {
            RoomId = roomId,
            roomState.MovieSlug,
            roomState.EpisodeNumber,
            roomState.IsPlaying,
            roomState.CurrentTime,
            ActiveUsers = users.Values.Select(u => u.Username).Distinct().ToList()
        });

        // Notify others in room
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", new
        {
            Username = username,
            ActiveCount = users.Count
        });
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        if (RoomUsers.TryGetValue(roomId, out var users))
        {
            if (users.TryRemove(Context.ConnectionId, out var user))
            {
                await Clients.Group(roomId).SendAsync("UserLeft", new
                {
                    Username = user.Username,
                    ActiveCount = users.Count
                });
            }
        }
    }

    public async Task SyncPlay(string roomId, double currentTime)
    {
        if (Rooms.TryGetValue(roomId, out var state))
        {
            state.IsPlaying = true;
            state.CurrentTime = currentTime;
            state.LastUpdated = DateTimeOffset.UtcNow;
        }

        await Clients.OthersInGroup(roomId).SendAsync("PlaybackSynced", new
        {
            Action = "play",
            CurrentTime = currentTime,
            SenderId = Context.ConnectionId
        });
    }

    public async Task SyncPause(string roomId, double currentTime)
    {
        if (Rooms.TryGetValue(roomId, out var state))
        {
            state.IsPlaying = false;
            state.CurrentTime = currentTime;
            state.LastUpdated = DateTimeOffset.UtcNow;
        }

        await Clients.OthersInGroup(roomId).SendAsync("PlaybackSynced", new
        {
            Action = "pause",
            CurrentTime = currentTime,
            SenderId = Context.ConnectionId
        });
    }

    public async Task SyncSeek(string roomId, double currentTime)
    {
        if (Rooms.TryGetValue(roomId, out var state))
        {
            state.CurrentTime = currentTime;
            state.LastUpdated = DateTimeOffset.UtcNow;
        }

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
        foreach (var (roomId, users) in RoomUsers)
        {
            if (users.TryRemove(Context.ConnectionId, out var user))
            {
                await Clients.Group(roomId).SendAsync("UserLeft", new
                {
                    Username = user.Username,
                    ActiveCount = users.Count
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
