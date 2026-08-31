using ZMovie.Application.Common;
using ZMovie.Domain.Catalog;

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

public interface ITitleRepository
{
    Task<Title?> FindByIdAsync(TitleId id, CancellationToken ct);
    Task<Title?> FindBySlugAsync(TitleSlug slug, CancellationToken ct);
    void Add(Title title);
    void Remove(Title title);
    Task SaveChangesAsync(CancellationToken ct);
    Task SyncGenreAssignmentsAsync(TitleId titleId, IReadOnlyList<GenreId> genreIds, DateTimeOffset occurredAt, CancellationToken ct);
    Task<IReadOnlyList<GenreId>> GetAssignedGenreIdsAsync(TitleId titleId, CancellationToken ct);
}

public interface IEpisodeRepository
{
    Task<IReadOnlyList<Episode>> ListByTitleIdAsync(TitleId titleId, CancellationToken ct);
    Task<Episode?> FindByNumberAsync(TitleId titleId, int number, CancellationToken ct);
    void Add(Episode episode);
    void Remove(Episode episode);
    Task SaveChangesAsync(CancellationToken ct);
    Task RemoveByTitleIdAsync(TitleId titleId, CancellationToken ct);
}

public interface IGenreRepository
{
    Task<Genre?> FindByIdAsync(GenreId id, CancellationToken ct);
    Task<Genre?> FindBySlugAsync(string slug, CancellationToken ct);
    Task<Genre?> FindByNameAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<Genre>> ListAllAsync(CancellationToken ct);
    void Add(Genre genre);
    void Remove(Genre genre);
    Task SaveChangesAsync(CancellationToken ct);
}
