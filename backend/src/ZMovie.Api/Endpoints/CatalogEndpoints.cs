using MediatR;
using System.Security.Claims;
using ZMovie.Api;
using ZMovie.Application.Analytics;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;

namespace ZMovie.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var catalog = endpoints.MapGroup("/v1/catalog").WithTags("Catalog");

        catalog.MapGet("/titles", async (ISender sender, string? q, string? genre, string? locale, CancellationToken ct) =>
                (await sender.Send(new ListTitlesQuery(q, genre, locale), ct)).ToApiResult())
            .WithName("ListCatalogTitles")
            .Produces<TitleListResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapGet("/titles/{slug}", async (ISender sender, string slug, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetTitleQuery(slug, locale), ct)).ToApiResult())
            .WithName("GetCatalogTitle")
            .Produces<TitleDetail>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapGet("/genres", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetGenresQuery(), ct)).ToApiResult())
            .Produces<List<string>>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapGet("/titles/{slug}/playback", async (ISender sender, string slug, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetPlaybackQuery(slug, locale), ct)).ToApiResult())
            .Produces<PlaybackResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapPost("/titles/{slug}/views", async (ISender sender, HttpContext context, string slug, RecordTitleViewRequest request, CancellationToken ct) =>
                (await sender.Send(new RecordTitleViewCommand(slug, UserIdentityAdapter.GetUserIdOrNull(context.User), UserIdentityAdapter.GetOrCreateAnalyticsSessionId(context), request.EpisodeNumber), ct)).ToApiResult())
            .Produces<ViewRecordedResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapGet("/titles/{slug}/reviews", async (ISender sender, string slug, CancellationToken ct) =>
                (await sender.Send(new GetTitleReviewsQuery(slug), ct)).ToApiResult())
            .Produces<TitleReviewsResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapPost("/titles/{slug}/reports", async (ISender sender, string slug, TitleReportRequest request, CancellationToken ct) =>
                (await sender.Send(new ReportTitleIssueCommand(slug, request.Category, request.Description, request.TimestampSeconds), ct)).ToApiResult())
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        return endpoints;
    }
}

public sealed record RecordTitleViewRequest(int? EpisodeNumber);
public sealed record TitleReportRequest(string Category, string Description, double? TimestampSeconds);
