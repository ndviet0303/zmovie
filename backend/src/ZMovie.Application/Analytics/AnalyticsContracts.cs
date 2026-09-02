using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Domain.Analytics;

namespace ZMovie.Application.Analytics;

public sealed record ViewRecordedResponse(long ViewCount, bool Counted);
public sealed record TopTitleResponse(TitleSummary Title, long Views);
public sealed record TopViewCount(Guid TitleId, long Views);

public interface IViewEventRepository
{
    Task<ViewRecordedResponse> RecordViewWithLockAsync(
        TitleId titleId,
        int? episodeNumber,
        UserId? userId,
        string sessionId,
        DateTimeOffset occurredAt,
        CancellationToken ct);

    Task<long> GetViewCountAsync(TitleId titleId, CancellationToken ct);
}

public interface IViewAnalyticsQueries
{
    Task<long> GetViewCountAsync(TitleId titleId, CancellationToken ct);
    Task<IReadOnlyList<TopViewCount>> GetTopAsync(TopPeriod period, int limit, DateTimeOffset now, CancellationToken ct);
}

public interface ITopTitlesResponseCache
{
    Task<IReadOnlyList<TopTitleResponse>> GetOrCreateAsync(
        TopPeriod period,
        string locale,
        int limit,
        Func<CancellationToken, Task<IReadOnlyList<TopTitleResponse>>> factory,
        CancellationToken ct);
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

public sealed class RecordTitleViewHandler(
    IViewEventRepository repository,
    ILibraryCatalogReader catalog,
    TimeProvider timeProvider) : IRequestHandler<RecordTitleViewCommand, ErrorOr<ViewRecordedResponse>>
{
    public async Task<ErrorOr<ViewRecordedResponse>> Handle(RecordTitleViewCommand request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null)
        {
            return Error.NotFound("catalog.title.not_found", "Catalog title not found.");
        }

        var userId = request.UserId.HasValue ? new UserId(request.UserId.Value) : (UserId?)null;
        var typedTitleId = new TitleId(titleId.Value);
        var now = timeProvider.GetUtcNow();

        return await repository.RecordViewWithLockAsync(
            typedTitleId,
            request.EpisodeNumber,
            userId,
            request.SessionId,
            now,
            ct);
    }
}

public sealed record GetTopTitlesQuery(TopPeriod Period, string? Locale, int Limit) : IQuery<IReadOnlyList<TopTitleResponse>>;

public sealed class GetTopTitlesHandler(
    IViewAnalyticsQueries queries,
    ILibraryCatalogReader catalog,
    ITopTitlesResponseCache cache,
    TimeProvider timeProvider) : IRequestHandler<GetTopTitlesQuery, ErrorOr<IReadOnlyList<TopTitleResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<TopTitleResponse>>> Handle(GetTopTitlesQuery request, CancellationToken ct)
    {
        var locale = Locale.Normalize(request.Locale);
        var now = timeProvider.GetUtcNow();

        return (await cache.GetOrCreateAsync(request.Period, locale, request.Limit, async token =>
        {
            var ranked = await queries.GetTopAsync(request.Period, request.Limit, now, token);
            var titles = await catalog.GetTitlesAsync(ranked.Select(x => x.TitleId), locale, token);
            return ranked.Where(x => titles.ContainsKey(x.TitleId))
                .Select(x => new TopTitleResponse(ToSummary(titles[x.TitleId]), x.Views)).ToList();
        }, ct)).ToList();
    }

    private static TitleSummary ToSummary(LibraryTitle title) =>
        new(title.Slug, title.Title, title.Genre, title.Year, title.Type, title.PosterUrl);
}

public sealed record GetTitleViewCountQuery(string Slug) : IQuery<long>;

public sealed class GetTitleViewCountHandler(
    IViewAnalyticsQueries queries,
    ILibraryCatalogReader catalog) : IRequestHandler<GetTitleViewCountQuery, ErrorOr<long>>
{
    public async Task<ErrorOr<long>> Handle(GetTitleViewCountQuery request, CancellationToken ct)
    {
        var titleId = await catalog.FindTitleIdAsync(request.Slug, ct);
        if (titleId is null)
        {
            return Error.NotFound("catalog.title.not_found", "Catalog title not found.");
        }

        return await queries.GetViewCountAsync(new TitleId(titleId.Value), ct);
    }
}
