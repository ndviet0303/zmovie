namespace ZMovie.Api.Endpoints;

public static class EndpointConventions
{
    public static RouteHandlerBuilder ProducesApiErrors(this RouteHandlerBuilder endpoint) => endpoint
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
}
