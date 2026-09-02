using ZMovie.Domain.Common;

namespace ZMovie.Domain.Catalog;

public sealed class EpisodeStreamSource : IEntity<EpisodeSourceId>
{
    private EpisodeStreamSource() { }

    public EpisodeSourceId Id { get; private set; }
    public EpisodeId EpisodeId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Format { get; private set; } = "hls";
    public int Priority { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;
    public string? SubtitleUrl { get; private set; }
    public string? AudioTrack { get; private set; }

    public static EpisodeStreamSource Create(
        EpisodeSourceId id,
        EpisodeId episodeId,
        string provider,
        string url,
        string format = "hls",
        int priority = 1,
        bool isActive = true,
        string? subtitleUrl = null,
        string? audioTrack = null)
    {
        return new EpisodeStreamSource
        {
            Id = id,
            EpisodeId = episodeId,
            Provider = provider?.Trim() ?? string.Empty,
            Url = url?.Trim() ?? string.Empty,
            Format = string.IsNullOrWhiteSpace(format) ? "hls" : format.Trim().ToLowerInvariant(),
            Priority = priority,
            IsActive = isActive,
            SubtitleUrl = subtitleUrl?.Trim(),
            AudioTrack = audioTrack?.Trim()
        };
    }

    public void UpdateStatus(bool isActive)
    {
        IsActive = isActive;
    }

    public void UpdateDetails(string url, string format, int priority, string? subtitleUrl = null, string? audioTrack = null)
    {
        Url = url?.Trim() ?? string.Empty;
        Format = string.IsNullOrWhiteSpace(format) ? "hls" : format.Trim().ToLowerInvariant();
        Priority = priority;
        SubtitleUrl = subtitleUrl?.Trim();
        AudioTrack = audioTrack?.Trim();
    }
}
