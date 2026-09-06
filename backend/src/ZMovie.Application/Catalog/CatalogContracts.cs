using ZMovie.Application.Common;
using ZMovie.Domain.Catalog;

namespace ZMovie.Application.Catalog;

public sealed record TitleSummary(string Slug, string Title, string Genre, int Year, string Type, string PosterUrl, bool IsR2Hosted = false, string Country = "");
public sealed record TitleDetail(string Slug, string Title, string Synopsis, string Genre, int Year, string Type, string PosterUrl, int RuntimeMinutes, long ViewCount, string Actors = "", string Directors = "", string Country = "", string TrailerUrl = "", bool IsR2Hosted = false);
public sealed record TitleListResponse(IReadOnlyList<TitleSummary> Items, int Total);
public sealed record HomeResponse(TitleSummary Hero, IReadOnlyList<TitleSummary> Trending);
public sealed record PlaybackSource(string Provider, string Url, string Format, int Priority, string? SubtitleUrl = null, string? AudioTrack = null);
public sealed record PlaybackMilestonesDto(int? IntroStart, int? IntroEnd, int? OutroStart, int? OutroEnd);

public sealed record PlaybackResponse(string Slug, string Title, bool IsSeries, IReadOnlyList<PlaybackEpisode> Episodes);
public sealed record PlaybackEpisode(
    int Number,
    string Name,
    string HlsUrl,
    string SubtitleUrl = "",
    IReadOnlyList<PlaybackSource>? Sources = null,
    PlaybackMilestonesDto? Milestones = null);
public sealed record ScheduleEntry(string Slug, string Title, string PosterUrl, DateOnly Date, int? EpisodeNumber);
public sealed record ScheduleResponse(DateOnly WeekStart, IReadOnlyList<ScheduleEntry> Items);
public sealed record PersonSummary(string Slug, string Name, IReadOnlyList<string> Roles, int TitleCount);
public sealed record PeopleResponse(IReadOnlyList<PersonSummary> Items, int Total);
public sealed record PersonDetail(string Slug, string Name, IReadOnlyList<string> Roles, IReadOnlyList<TitleSummary> Titles);

public interface ICatalogReadStore
{
    Task<TitleListResponse> ListAsync(string? query, string? genre, string? country, int? year, string? type, string? sort, int page, int pageSize, string locale, CancellationToken ct);
    Task<TitleDetail?> GetAsync(string slug, string locale, CancellationToken ct);
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct);
    Task<PlaybackResponse?> GetPlaybackAsync(string slug, string locale, CancellationToken ct);
    Task<HomeResponse?> GetHomeAsync(string locale, CancellationToken ct);
    Task<ScheduleResponse> GetScheduleAsync(DateOnly weekStart, string locale, CancellationToken ct);
    Task<PeopleResponse> ListPeopleAsync(string? query, int page, int pageSize, CancellationToken ct);
    Task<PersonDetail?> GetPersonAsync(string slug, string locale, CancellationToken ct);
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
