namespace ZMovie.Domain.Catalog;

public static class StreamFormat
{
    public const string Hls = "hls";
    public const string Video = "video";
    public const string Embed = "embed";

    private static readonly string[] DirectVideoExtensions = [".mp4", ".webm", ".ogg", ".m4v"];

    public static string Infer(string? url, string? fallback = null)
    {
        var pathname = GetPathname(url);
        if (pathname.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)) return Hls;
        if (DirectVideoExtensions.Any(extension => pathname.EndsWith(extension, StringComparison.OrdinalIgnoreCase))) return Video;

        return fallback?.Trim().ToLowerInvariant() switch
        {
            Hls => Hls,
            Video or "mp4" => Video,
            Embed => Embed,
            _ => Embed,
        };
    }

    private static string GetPathname(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed)) return parsed.AbsolutePath;
        return url.Trim().Split(['?', '#'], 2)[0];
    }
}
