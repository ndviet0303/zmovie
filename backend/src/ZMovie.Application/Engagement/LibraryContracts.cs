using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Application.Catalog;
using ZMovie.Domain.Engagement;

namespace ZMovie.Application.Engagement;

public sealed record LibraryTitle(string Slug, string Title, string Genre, int Year, string Type, string PosterUrl, int RuntimeMinutes);
public sealed record WatchHistoryTitle(LibraryTitle Title, int? EpisodeNumber, double ProgressSeconds, DateTimeOffset UpdatedAt);
public sealed record UserLibraryResponse(IReadOnlyList<LibraryTitle> Saved, IReadOnlyList<WatchHistoryTitle> History);
public sealed record SavedTitleEntry(Guid TitleId, DateTimeOffset SavedAt);
public sealed record WatchProgressEntry(Guid TitleId, Guid PlayableId, int? EpisodeNumber, double ProgressSeconds, DateTimeOffset UpdatedAt);
public sealed record PlayableReference(Guid TitleId, Guid PlayableId, int? EpisodeNumber);
public sealed record RecommendationCandidate(Guid TitleId, LibraryTitle Title, string Synopsis);
public sealed record RecommendationSeed(Guid TitleId, int Weight);
public sealed record ContinueWatchingTitle(TitleSummary Title, int? EpisodeNumber, double ProgressSeconds, DateTimeOffset UpdatedAt);
public sealed record PersonalizedDiscoveryResponse(IReadOnlyList<ContinueWatchingTitle> ContinueWatching, IReadOnlyList<TitleSummary> Recommended);

public interface ILibraryCatalogReader
{
    Task<Guid?> FindTitleIdAsync(string slug, CancellationToken ct);
    Task<PlayableReference?> FindPlayableAsync(string slug, int? episodeNumber, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, LibraryTitle>> GetTitlesAsync(IEnumerable<Guid> titleIds, string locale, CancellationToken ct);
    Task<IReadOnlyList<LibraryTitle>> GetDiscoveryTitlesAsync(string locale, CancellationToken ct);
    Task<IReadOnlyList<RecommendationCandidate>> GetRecommendationCandidatesAsync(string locale, CancellationToken ct);
}

public interface IRecommendationEngine
{
    IReadOnlyList<Guid> Recommend(IReadOnlyList<RecommendationCandidate> candidates, IReadOnlyList<RecommendationSeed> profile, IReadOnlySet<Guid> excludedTitleIds, int limit);
}

public sealed record GetUserLibraryQuery(Guid UserId, string Locale) : IQuery<UserLibraryResponse>;
public sealed class GetUserLibraryHandler(IUserLibraryQueries queries, ILibraryCatalogReader catalog) : IRequestHandler<GetUserLibraryQuery, ErrorOr<UserLibraryResponse>>
{
    public async Task<ErrorOr<UserLibraryResponse>> Handle(GetUserLibraryQuery request, CancellationToken ct)
    {
        var savedEntries = await queries.ListSavedAsync(new UserId(request.UserId), ct);
        var historyEntries = await queries.ListHistoryAsync(new UserId(request.UserId), ct);
        var titles = await catalog.GetTitlesAsync(savedEntries.Select(x => x.TitleId).Concat(historyEntries.Select(x => x.TitleId)).Distinct(), request.Locale, ct);
        var saved = savedEntries.Where(x => titles.ContainsKey(x.TitleId)).Select(x => titles[x.TitleId]).ToList();
        var history = historyEntries.Where(x => titles.ContainsKey(x.TitleId)).Select(x => new WatchHistoryTitle(titles[x.TitleId], x.EpisodeNumber, x.ProgressSeconds, x.UpdatedAt)).ToList();
        return new UserLibraryResponse(saved, history);
    }
}

public sealed record GetPersonalizedDiscoveryQuery(Guid UserId, string Locale) : IQuery<PersonalizedDiscoveryResponse>;
public sealed class GetPersonalizedDiscoveryHandler(IUserLibraryQueries queries, ILibraryCatalogReader catalog, IRecommendationEngine recommender) : IRequestHandler<GetPersonalizedDiscoveryQuery, ErrorOr<PersonalizedDiscoveryResponse>>
{
    public async Task<ErrorOr<PersonalizedDiscoveryResponse>> Handle(GetPersonalizedDiscoveryQuery request, CancellationToken ct)
    {
        var saved = await queries.ListSavedAsync(new UserId(request.UserId), ct);
        var history = await queries.ListHistoryAsync(new UserId(request.UserId), ct);
        var referencedIds = saved.Select(x => x.TitleId).Concat(history.Select(x => x.TitleId)).Distinct().ToArray();
        var referencedTitles = await catalog.GetTitlesAsync(referencedIds, request.Locale, ct);
        var continueWatching = history.Where(x => referencedTitles.ContainsKey(x.TitleId)).Take(5)
            .Select(x => new ContinueWatchingTitle(ToSummary(referencedTitles[x.TitleId]), x.EpisodeNumber, x.ProgressSeconds, x.UpdatedAt)).ToList();
        var candidates = await catalog.GetRecommendationCandidatesAsync(request.Locale, ct);
        var profile = saved.Select(x => new RecommendationSeed(x.TitleId, 1))
            .Concat(history.Select(x => new RecommendationSeed(x.TitleId, 3))).ToList();
        var excluded = saved.Select(x => x.TitleId).Concat(history.Select(x => x.TitleId)).ToHashSet();
        var candidateById = candidates.ToDictionary(x => x.TitleId);
        var recommendations = recommender.Recommend(candidates, profile, excluded, 5)
            .Where(candidateById.ContainsKey).Select(id => ToSummary(candidateById[id].Title)).ToList();
        return new PersonalizedDiscoveryResponse(continueWatching, recommendations);
    }

    private static TitleSummary ToSummary(LibraryTitle title) => new(title.Slug, title.Title, title.Genre, title.Year, title.Type, title.PosterUrl);
}

public sealed record SaveTitleCommand(Guid UserId, string Slug) : ICommand<bool>;
public sealed class SaveTitleHandler(
    ISavedTitleRepository repository,
    ILibraryCatalogReader catalog,
    TimeProvider timeProvider) : IRequestHandler<SaveTitleCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(SaveTitleCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");

        var userId = new UserId(request.UserId);
        var typedTitleId = new TitleId(titleId.Value);
        var existing = await repository.FindAsync(userId, typedTitleId, ct);
        if (existing is null)
        {
            repository.Add(SavedTitle.Create(userId, typedTitleId, timeProvider.GetUtcNow()));
            await repository.SaveChangesAsync(ct);
        }

        return true;
    }
}

public sealed record RemoveSavedTitleCommand(Guid UserId, string Slug) : ICommand<bool>;
public sealed class RemoveSavedTitleHandler(
    ISavedTitleRepository repository,
    ILibraryCatalogReader catalog) : IRequestHandler<RemoveSavedTitleCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RemoveSavedTitleCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("engagement.saved.not_found", "Saved title not found.");

        var userId = new UserId(request.UserId);
        var typedTitleId = new TitleId(titleId.Value);
        var existing = await repository.FindAsync(userId, typedTitleId, ct);
        if (existing is null) return Error.NotFound("engagement.saved.not_found", "Saved title not found.");

        repository.Remove(existing);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record RecordWatchProgressCommand(Guid UserId, string Slug, int? EpisodeNumber, double ProgressSeconds) : ICommand<bool>;
public sealed class RecordWatchProgressHandler(
    IWatchProgressRepository repository,
    ILibraryCatalogReader catalog,
    TimeProvider timeProvider) : IRequestHandler<RecordWatchProgressCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RecordWatchProgressCommand request, CancellationToken ct)
    {
        var playable = await catalog.FindPlayableAsync(request.Slug, request.EpisodeNumber, ct);
        if (playable is null) return Error.NotFound("catalog.playable.not_found", "Catalog playable not found.");

        var userId = new UserId(request.UserId);
        var playableId = new PlayableId(playable.PlayableId);
        var titleId = new TitleId(playable.TitleId);
        var position = WatchPosition.FromSeconds(request.ProgressSeconds);
        var now = timeProvider.GetUtcNow();

        var existing = await repository.FindAsync(userId, playableId, ct);
        if (existing is null)
        {
            repository.Add(WatchProgress.Record(userId, playableId, titleId, playable.EpisodeNumber, position, now));
        }
        else
        {
            existing.UpdateProgress(playable.EpisodeNumber, position, now);
        }

        await repository.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record RemoveWatchHistoryCommand(Guid UserId, string Slug) : ICommand<bool>;
public sealed class RemoveWatchHistoryHandler(
    IWatchProgressRepository repository,
    ILibraryCatalogReader catalog) : IRequestHandler<RemoveWatchHistoryCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RemoveWatchHistoryCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");

        await repository.RemoveByTitleAsync(new UserId(request.UserId), new TitleId(titleId.Value), ct);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record ClearWatchHistoryCommand(Guid UserId) : ICommand<bool>;
public sealed class ClearWatchHistoryHandler(
    IWatchProgressRepository repository) : IRequestHandler<ClearWatchHistoryCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(ClearWatchHistoryCommand request, CancellationToken ct)
    {
        await repository.ClearAllAsync(new UserId(request.UserId), ct);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
