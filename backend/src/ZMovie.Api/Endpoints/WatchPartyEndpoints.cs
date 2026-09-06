using ZMovie.Application.Catalog;
using ZMovie.Application.WatchParty;

namespace ZMovie.Api.Endpoints;

public static class WatchPartyEndpoints
{
    public static IEndpointRouteBuilder MapWatchPartyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/v1/watch-parties", (IWatchPartyRegistry registry) => Results.Ok(registry.List()))
            .WithTags("Watch parties")
            .WithName("ListWatchParties")
            .Produces<IReadOnlyList<WatchPartyRoom>>(StatusCodes.Status200OK);

        endpoints.MapPost("/v1/watch-parties", async (IWatchPartyRegistry registry, ICatalogReadStore catalog, CreateWatchPartyRequest request, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.MovieSlug) || request.EpisodeNumber < 1)
                return Results.BadRequest(new { code = "watch_party.invalid_request", message = "Movie and episode are required." });
            if (await catalog.GetAsync(request.MovieSlug.Trim(), "vi", ct) is null)
                return Results.NotFound(new { code = "catalog.title.not_found", message = "Catalog title not found." });

            return Results.Ok(registry.Create(request.MovieSlug.Trim(), request.EpisodeNumber));
        })
            .WithTags("Watch parties")
            .WithName("CreateWatchParty")
            .Produces<CreatedWatchPartyRoom>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapDelete("/v1/watch-parties/{roomId}", (IWatchPartyRegistry registry, string roomId, string managementToken) =>
                registry.Delete(roomId, managementToken) ? Results.NoContent() : Results.Unauthorized())
            .WithTags("Watch parties")
            .WithName("DeleteWatchParty")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}

public sealed record CreateWatchPartyRequest(string MovieSlug, int EpisodeNumber);
