using Microsoft.AspNetCore.SignalR;

namespace ZMovie.Infrastructure.Realtime;

public sealed record DanmakuBroadcastDto(
    string Id,
    string TitleSlug,
    int EpisodeNumber,
    int TimeSeconds,
    string Content,
    string Color,
    string AuthorName,
    DateTimeOffset CreatedAt);

public sealed class DanmakuHub : Hub
{
    public static string GetGroupName(string slug, int episodeNumber) => $"danmaku_{slug}_{episodeNumber}";

    public async Task JoinPlayback(string slug, int episodeNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(slug, episodeNumber));
    }

    public async Task LeavePlayback(string slug, int episodeNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(slug, episodeNumber));
    }

    public async Task SendDanmaku(string slug, int episodeNumber, int timeSeconds, string content, string color, string authorName)
    {
        var dto = new DanmakuBroadcastDto(
            Guid.NewGuid().ToString("N"),
            slug,
            episodeNumber,
            timeSeconds,
            content?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(color) ? "#ffffff" : color.Trim(),
            string.IsNullOrWhiteSpace(authorName) ? "Anonymous" : authorName.Trim(),
            DateTimeOffset.UtcNow);

        await Clients.Group(GetGroupName(slug, episodeNumber)).SendAsync("DanmakuReceived", dto);
    }
}
