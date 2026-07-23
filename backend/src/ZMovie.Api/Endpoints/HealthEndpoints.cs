namespace ZMovie.Api.Endpoints;

public sealed record HealthResponse(string Status);

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new HealthResponse("live")))
            .Produces<HealthResponse>(StatusCodes.Status200OK);
        endpoints.MapGet("/health/ready", () => Results.Ok(new HealthResponse("ready")))
            .Produces<HealthResponse>(StatusCodes.Status200OK);

        return endpoints;
    }
}
