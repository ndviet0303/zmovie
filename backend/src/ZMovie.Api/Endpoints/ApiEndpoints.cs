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
            .MapBillingEndpoints()
            .MapWatchPartyEndpoints()
            .MapHealthEndpoints();

        return endpoints;
    }
}
