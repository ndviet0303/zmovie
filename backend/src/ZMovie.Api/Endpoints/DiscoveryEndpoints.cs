using MediatR;
using System.Security.Claims;
using ZMovie.Api;
using ZMovie.Application.Catalog;
using ZMovie.Application.Engagement;

namespace ZMovie.Api.Endpoints;

public static class DiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/v1/discovery/home", async (ISender sender, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetHomeQuery(locale), ct)).ToApiResult())
            .Produces<HomeResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        endpoints.MapGet("/v1/discovery/top/{period}", async (ISender sender, string period, string? locale, int? limit, CancellationToken ct) =>
        {
            if (!Enum.TryParse<TopPeriod>(period, true, out var topPeriod))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["period"] = ["Use day, week, or month."] });
            return (await sender.Send(new GetTopTitlesQuery(topPeriod, locale, Math.Clamp(limit ?? 10, 1, 50)), ct)).ToApiResult();
        })
            .Produces<IReadOnlyList<TopTitleResponse>>(StatusCodes.Status200OK)
            .ProducesApiErrors();
        endpoints.MapGet("/v1/discovery/for-you", async (ISender sender, HttpContext context, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetPersonalizedDiscoveryQuery(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) is true ? "en" : "vi"), ct)).ToApiResult())
            .RequireAuthorization()
            .Produces<PersonalizedDiscoveryResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        return endpoints;
    }
}
