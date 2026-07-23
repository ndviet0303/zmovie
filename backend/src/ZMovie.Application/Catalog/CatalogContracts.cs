using ZMovie.Application.Common;

namespace ZMovie.Application.Catalog;

public sealed record TitleSummary(string Slug, string Title, string Genre, int Year, string Type, string PosterUrl);
public sealed record TitleDetail(string Slug, string Title, string Synopsis, string Genre, int Year, string Type, string PosterUrl, int RuntimeMinutes, long ViewCount);
public sealed record TitleListResponse(IReadOnlyList<TitleSummary> Items, int Total);
public sealed record HomeResponse(TitleSummary Hero, IReadOnlyList<TitleSummary> Trending);
public sealed record PlaybackResponse(string Slug, string Title, bool IsSeries, IReadOnlyList<PlaybackEpisode> Episodes);
public sealed record PlaybackEpisode(int Number, string Name, string HlsUrl);
public interface ICatalogReadStore
{
    Task<TitleListResponse> ListAsync(string? query, string? genre, string locale, CancellationToken ct);
    Task<TitleDetail?> GetAsync(string slug, string locale, CancellationToken ct);
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct);
    Task<PlaybackResponse?> GetPlaybackAsync(string slug, string locale, CancellationToken ct);
    Task<HomeResponse?> GetHomeAsync(string locale, CancellationToken ct);
}
