using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Application.Catalog;

namespace ZMovie.Application.Engagement;

public sealed record LibraryTitle(string Slug, string Title, string Genre, int Year, string Type, string PosterUrl, int RuntimeMinutes);
public sealed record WatchHistoryTitle(LibraryTitle Title, int? EpisodeNumber, double ProgressSeconds, DateTimeOffset UpdatedAt);
public sealed record UserLibraryResponse(IReadOnlyList<LibraryTitle> Saved, IReadOnlyList<WatchHistoryTitle> History);
public sealed record SavedTitleEntry(Guid TitleId, DateTimeOffset SavedAt);
public sealed record WatchProgressEntry(Guid TitleId, Guid PlayableId, int? EpisodeNumber, double ProgressSeconds, DateTimeOffset UpdatedAt);
public sealed record PlayableReference(Guid TitleId, Guid PlayableId, int? EpisodeNumber);
public sealed record ViewRecordedResponse(long ViewCount, bool Counted);
public sealed record TopTitleResponse(TitleSummary Title, long Views);
public sealed record TopViewCount(Guid TitleId, long Views);
public sealed record RecommendationCandidate(Guid TitleId, LibraryTitle Title, string Synopsis);
public sealed record RecommendationSeed(Guid TitleId, int Weight);
public sealed record ContinueWatchingTitle(TitleSummary Title, int? EpisodeNumber, double ProgressSeconds, DateTimeOffset UpdatedAt);
public sealed record PersonalizedDiscoveryResponse(IReadOnlyList<ContinueWatchingTitle> ContinueWatching, IReadOnlyList<TitleSummary> Recommended);

public enum TopPeriod { Day, Week, Month }

public interface IUserLibraryStore
{
    Task<IReadOnlyList<SavedTitleEntry>> GetSavedAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<WatchProgressEntry>> GetHistoryAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(Guid userId, Guid titleId, CancellationToken ct);
    Task<bool> RemoveAsync(Guid userId, Guid titleId, CancellationToken ct);
    Task RecordProgressAsync(Guid userId, PlayableReference playable, double progressSeconds, CancellationToken ct);
}

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

public interface IViewAnalyticsStore
{
    Task<ViewRecordedResponse> RecordAsync(Guid titleId, Guid? userId, string sessionId, int? episodeNumber, CancellationToken ct);
    Task<long> GetViewCountAsync(Guid titleId, CancellationToken ct);
    Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, CancellationToken ct);
}

public interface ITopTitlesResponseCache
{
    Task<IReadOnlyList<TopTitleResponse>> GetOrCreateAsync(TopPeriod period, string locale, int limit, Func<CancellationToken, Task<IReadOnlyList<TopTitleResponse>>> factory, CancellationToken ct);
}

public sealed record GetUserLibraryQuery(Guid UserId, string Locale) : IQuery<UserLibraryResponse>;
public sealed class GetUserLibraryHandler(IUserLibraryStore store, ILibraryCatalogReader catalog) : IRequestHandler<GetUserLibraryQuery, ErrorOr<UserLibraryResponse>>
{
    public async Task<ErrorOr<UserLibraryResponse>> Handle(GetUserLibraryQuery request, CancellationToken ct)
    {
        var savedEntries = await store.GetSavedAsync(request.UserId, ct);
        var historyEntries = await store.GetHistoryAsync(request.UserId, ct);
        var titles = await catalog.GetTitlesAsync(savedEntries.Select(x => x.TitleId).Concat(historyEntries.Select(x => x.TitleId)).Distinct(), request.Locale, ct);
        var saved = savedEntries.Where(x => titles.ContainsKey(x.TitleId)).Select(x => titles[x.TitleId]).ToList();
        var history = historyEntries.Where(x => titles.ContainsKey(x.TitleId)).Select(x => new WatchHistoryTitle(titles[x.TitleId], x.EpisodeNumber, x.ProgressSeconds, x.UpdatedAt)).ToList();
        return new UserLibraryResponse(saved, history);
    }
}

public sealed record GetPersonalizedDiscoveryQuery(Guid UserId, string Locale) : IQuery<PersonalizedDiscoveryResponse>;
public sealed class GetPersonalizedDiscoveryHandler(IUserLibraryStore store, ILibraryCatalogReader catalog, IRecommendationEngine recommender) : IRequestHandler<GetPersonalizedDiscoveryQuery, ErrorOr<PersonalizedDiscoveryResponse>>
{
    public async Task<ErrorOr<PersonalizedDiscoveryResponse>> Handle(GetPersonalizedDiscoveryQuery request, CancellationToken ct)
    {
        var saved = await store.GetSavedAsync(request.UserId, ct);
        var history = await store.GetHistoryAsync(request.UserId, ct);
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
public sealed class SaveTitleHandler(IUserLibraryStore store, ILibraryCatalogReader catalog) : IRequestHandler<SaveTitleCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(SaveTitleCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");
        await store.SaveAsync(request.UserId, titleId.Value, ct);
        return true;
    }
}

public sealed record RemoveSavedTitleCommand(Guid UserId, string Slug) : ICommand<bool>;
public sealed class RemoveSavedTitleHandler(IUserLibraryStore store, ILibraryCatalogReader catalog) : IRequestHandler<RemoveSavedTitleCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RemoveSavedTitleCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null || !await store.RemoveAsync(request.UserId, titleId.Value, ct)) return Error.NotFound("engagement.saved.not_found", "Saved title not found.");
        return true;
    }
}

public sealed record RecordWatchProgressCommand(Guid UserId, string Slug, int? EpisodeNumber, double ProgressSeconds) : ICommand<bool>;
public sealed class RecordWatchProgressHandler(IUserLibraryStore store, ILibraryCatalogReader catalog) : IRequestHandler<RecordWatchProgressCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RecordWatchProgressCommand request, CancellationToken ct)
    {
        var playable = await catalog.FindPlayableAsync(request.Slug, request.EpisodeNumber, ct);
        if (playable is null) return Error.NotFound("catalog.playable.not_found", "Catalog playable not found.");
        await store.RecordProgressAsync(request.UserId, playable, request.ProgressSeconds, ct);
        return true;
    }
}

public sealed record RecordTitleViewCommand(string Slug, Guid? UserId, string SessionId, int? EpisodeNumber) : ICommand<ViewRecordedResponse>;
public sealed class RecordTitleViewValidator : AbstractValidator<RecordTitleViewCommand>
{
    public RecordTitleViewValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(160);
        RuleFor(x => x.SessionId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EpisodeNumber).GreaterThan(0).When(x => x.EpisodeNumber.HasValue);
    }
}
public sealed class RecordTitleViewHandler(IViewAnalyticsStore store, ILibraryCatalogReader catalog) : IRequestHandler<RecordTitleViewCommand, ErrorOr<ViewRecordedResponse>>
{
    public async Task<ErrorOr<ViewRecordedResponse>> Handle(RecordTitleViewCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        return titleId is null
            ? Error.NotFound("catalog.title.not_found", "Catalog title not found.")
            : await store.RecordAsync(titleId.Value, request.UserId, request.SessionId, request.EpisodeNumber, ct);
    }
}

public sealed record GetTopTitlesQuery(TopPeriod Period, string? Locale, int Limit) : IQuery<IReadOnlyList<TopTitleResponse>>;
public sealed class GetTopTitlesHandler(IViewAnalyticsStore store, ILibraryCatalogReader catalog, ITopTitlesResponseCache cache) : IRequestHandler<GetTopTitlesQuery, ErrorOr<IReadOnlyList<TopTitleResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<TopTitleResponse>>> Handle(GetTopTitlesQuery request, CancellationToken ct)
    {
        var locale = Locale.Normalize(request.Locale);
        return (await cache.GetOrCreateAsync(request.Period, locale, request.Limit, async token =>
        {
            var ranked = await store.GetTopAsync(request.Period, request.Limit, token);
            var titles = await catalog.GetTitlesAsync(ranked.Select(x => x.TitleId), locale, token);
            return ranked.Where(x => titles.ContainsKey(x.TitleId))
                .Select(x => new TopTitleResponse(ToSummary(titles[x.TitleId]), x.Views)).ToList();
        }, ct)).ToList();
    }

    private static TitleSummary ToSummary(LibraryTitle title) => new(title.Slug, title.Title, title.Genre, title.Year, title.Type, title.PosterUrl);
}
