using ErrorOr;

namespace ZMovie.Api;

public static class ApiResults
{
    public static IResult ToApiResult<T>(this ErrorOr<T> result) => result.Match<IResult>(Results.Ok, errors =>
    {
        var first = errors[0];
        var status = first.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
        return Results.Problem(statusCode: status, title: first.Description, extensions: new Dictionary<string, object?> { ["code"] = first.Code, ["errors"] = errors.Select(x => new { x.Code, x.Description }) });
    });
}
