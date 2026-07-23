using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;

namespace ZMovie.Application.Catalog;

public sealed record ListTitlesQuery(string? Query, string? Genre, string? Locale) : IQuery<TitleListResponse>;
public sealed class ListTitlesValidator : AbstractValidator<ListTitlesQuery>
{ public ListTitlesValidator() => RuleFor(x => x.Query).MaximumLength(200); }
public sealed class ListTitlesHandler(ICatalogReadStore store) : IRequestHandler<ListTitlesQuery, ErrorOr<TitleListResponse>>
{ public async Task<ErrorOr<TitleListResponse>> Handle(ListTitlesQuery request, CancellationToken ct) => await store.ListAsync(request.Query, request.Genre, Locale.Normalize(request.Locale), ct); }

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
