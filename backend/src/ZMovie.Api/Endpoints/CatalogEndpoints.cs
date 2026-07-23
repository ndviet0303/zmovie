using MediatR;
using System.Security.Claims;
using ZMovie.Api;
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
                (await sender.Send(new RecordTitleViewCommand(slug, UserIdOrNull(context), AnalyticsSessionId(context), request.EpisodeNumber), ct)).ToApiResult())
            .Produces<ViewRecordedResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        catalog.MapGet("/titles/{slug}/reviews", async (ISender sender, string slug, CancellationToken ct) =>
                (await sender.Send(new GetTitleReviewsQuery(slug), ct)).ToApiResult())
            .Produces<TitleReviewsResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        return endpoints;
    }

    private static Guid? UserIdOrNull(HttpContext context) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static string AnalyticsSessionId(HttpContext context)
    {
        const string cookieName = "zmovie.analytics-session";
        if (context.Request.Cookies.TryGetValue(cookieName, out var sessionId) && Guid.TryParse(sessionId, out _)) return sessionId;

        sessionId = Guid.CreateVersion7().ToString("N");
        context.Response.Cookies.Append(cookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(30),
        });
        return sessionId;
    }
}

public sealed record RecordTitleViewRequest(int? EpisodeNumber);
