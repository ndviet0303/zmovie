using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZMovie.Infrastructure.Operations;

public interface ITelegramAlertService
{
    Task SendStreamOutageAlertAsync(string titleSlug, int episodeNumber, string sourceUrl, int statusCode, CancellationToken ct = default);
    Task SendMessageAsync(string markdownMessage, CancellationToken ct = default);
}

public sealed class TelegramAlertService(HttpClient httpClient, IConfiguration config, ILogger<TelegramAlertService> logger) : ITelegramAlertService
{
    private readonly string? _botToken = config["Telegram:BotToken"] ?? config["TELEGRAM_BOT_TOKEN"];
    private readonly string? _chatId = config["Telegram:ChatId"] ?? config["TELEGRAM_CHAT_ID"];

    public async Task SendStreamOutageAlertAsync(string titleSlug, int episodeNumber, string sourceUrl, int statusCode, CancellationToken ct = default)
    {
        var message = $"""
            🚨 *[ZMOVIE STREAM HEALTH ALERT]*
            • *Phim:* `{titleSlug}`
            • *Tập:* {episodeNumber}
            • *Nguồn phát:* `{sourceUrl}`
            • *Mã phản hồi:* HTTP {statusCode}
            • *Trạng thái:* Đang tự động kích hoạt Self-Healing failover nguồn dự phòng.
            """;

        await SendMessageAsync(message, ct);
    }

    public async Task SendMessageAsync(string markdownMessage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_chatId))
        {
            logger.LogInformation("Telegram alert suppressed (credentials not configured): {Message}", markdownMessage);
            return;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new
            {
                chat_id = _chatId,
                text = markdownMessage,
                parse_mode = "Markdown"
            };

            var response = await httpClient.PostAsJsonAsync(url, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to send Telegram alert: HTTP {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while dispatching Telegram notification");
        }
    }
}
