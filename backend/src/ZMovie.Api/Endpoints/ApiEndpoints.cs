namespace ZMovie.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapAuthEndpoints()
            .MapCatalogEndpoints()
            .MapDiscoveryEndpoints()
            .MapAssistantEndpoints()
            .MapSearchEndpoints()
            .MapAdminEndpoints()
            .MapHealthEndpoints();

        return endpoints;
    }
}
