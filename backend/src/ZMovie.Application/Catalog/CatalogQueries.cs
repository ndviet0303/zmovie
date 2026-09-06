using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ZMovie.Application.Common;

namespace ZMovie.Application.Catalog;

public sealed record ListTitlesQuery(
    string? Query,
    string? Genre,
    string? Country,
    int? Year,
    string? Type,
    string? Sort,
    int Page,
    int PageSize,
    string? Locale) : IQuery<TitleListResponse>;
public sealed class ListTitlesValidator : AbstractValidator<ListTitlesQuery>
{
    public ListTitlesValidator()
    {
        RuleFor(x => x.Query).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThan(0);
        When(x => x.Year.HasValue, () =>
            RuleFor(x => x.Year).InclusiveBetween(1888, 2200));
        RuleFor(x => x.Type).Must(value => value is null or "movie" or "series" or "r2");
        RuleFor(x => x.Sort).Must(value => value is null or "latest" or "oldest" or "title");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
public sealed class ListTitlesHandler(ICatalogReadStore store) : IRequestHandler<ListTitlesQuery, ErrorOr<TitleListResponse>>
{
    public async Task<ErrorOr<TitleListResponse>> Handle(ListTitlesQuery request, CancellationToken ct) =>
        await store.ListAsync(
            request.Query,
            request.Genre,
            request.Country,
            request.Year,
            request.Type,
            request.Sort,
            request.Page,
            request.PageSize,
            Locale.Normalize(request.Locale),
            ct);
}

public sealed record GetTitleQuery(string Slug, string? Locale) : IQuery<TitleDetail>;
public sealed class GetTitleHandler(ICatalogReadStore store) : IRequestHandler<GetTitleQuery, ErrorOr<TitleDetail>>
{ public async Task<ErrorOr<TitleDetail>> Handle(GetTitleQuery request, CancellationToken ct) => await store.GetAsync(request.Slug, Locale.Normalize(request.Locale), ct) is { } item ? item : Error.NotFound("catalog.title.not_found", "Catalog title not found."); }

public sealed record GetGenresQuery : IQuery<List<string>>;
public sealed class GetGenresHandler(ICatalogReadStore store) : IRequestHandler<GetGenresQuery, ErrorOr<List<string>>>
{ public async Task<ErrorOr<List<string>>> Handle(GetGenresQuery request, CancellationToken ct) => (await store.GetGenresAsync(ct)).ToList(); }

public sealed record GetPlaybackQuery(string Slug, string? Locale) : IQuery<PlaybackResponse>;
public sealed class GetPlaybackHandler(ICatalogReadStore store) : IRequestHandler<GetPlaybackQuery, ErrorOr<PlaybackResponse>>
{ public async Task<ErrorOr<PlaybackResponse>> Handle(GetPlaybackQuery request, CancellationToken ct) => await store.GetPlaybackAsync(request.Slug, Locale.Normalize(request.Locale), ct) is { } item ? item : Error.NotFound("catalog.playback.not_found", "Playback not found."); }

public sealed record GetHomeQuery(string? Locale) : IQuery<HomeResponse>;
public sealed class GetHomeHandler(ICatalogReadStore store) : IRequestHandler<GetHomeQuery, ErrorOr<HomeResponse>>
{ public async Task<ErrorOr<HomeResponse>> Handle(GetHomeQuery request, CancellationToken ct) => await store.GetHomeAsync(Locale.Normalize(request.Locale), ct) is { } item ? item : Error.Failure("catalog.home.unavailable", "Discovery catalog is unavailable."); }

public sealed record GetScheduleQuery(DateOnly? WeekStart, string? Locale) : IQuery<ScheduleResponse>;
public sealed class GetScheduleHandler(ICatalogReadStore store, TimeProvider timeProvider) : IRequestHandler<GetScheduleQuery, ErrorOr<ScheduleResponse>>
{
    public async Task<ErrorOr<ScheduleResponse>> Handle(GetScheduleQuery request, CancellationToken ct)
    {
        var requestedDate = request.WeekStart ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var daysSinceMonday = ((int)requestedDate.DayOfWeek + 6) % 7;
        var weekStart = requestedDate.AddDays(-daysSinceMonday);
        return await store.GetScheduleAsync(weekStart, Locale.Normalize(request.Locale), ct);
    }
}


public sealed record ListPeopleQuery(string? Query, int Page, int PageSize) : IQuery<PeopleResponse>;
public sealed class ListPeopleValidator : AbstractValidator<ListPeopleQuery>
{
    public ListPeopleValidator()
    {
        RuleFor(x => x.Query).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
public sealed class ListPeopleHandler(ICatalogReadStore store) : IRequestHandler<ListPeopleQuery, ErrorOr<PeopleResponse>>
{
    public async Task<ErrorOr<PeopleResponse>> Handle(ListPeopleQuery request, CancellationToken ct) =>
        await store.ListPeopleAsync(request.Query, request.Page, request.PageSize, ct);
}

public sealed record GetPersonQuery(string Slug, string? Locale) : IQuery<PersonDetail>;
public sealed class GetPersonHandler(ICatalogReadStore store) : IRequestHandler<GetPersonQuery, ErrorOr<PersonDetail>>
{
    public async Task<ErrorOr<PersonDetail>> Handle(GetPersonQuery request, CancellationToken ct) =>
        await store.GetPersonAsync(request.Slug, Locale.Normalize(request.Locale), ct) is { } person
            ? person
            : Error.NotFound("catalog.person.not_found", "Person not found.");
}
public sealed record ReportTitleIssueCommand(string Slug, string Category, string Description, double? TimestampSeconds) : ICommand<bool>;
public sealed class ReportTitleIssueHandler(ICatalogReadStore store, Microsoft.Extensions.Logging.ILogger<ReportTitleIssueHandler> logger) : IRequestHandler<ReportTitleIssueCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(ReportTitleIssueCommand request, CancellationToken ct)
    {
        var title = await store.GetAsync(request.Slug, "vi", ct);
        if (title is null) return Error.NotFound("catalog.title.not_found", "Catalog title not found.");

        logger.LogWarning("Issue reported for {Slug} [{Category} at {Timestamp}s]: {Description}",
            request.Slug, request.Category, request.TimestampSeconds, request.Description);

        return true;
    }
}
