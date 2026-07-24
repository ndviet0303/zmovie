using MediatR;
using System.Security.Claims;
using ZMovie.Api;
using ZMovie.Application.Assistant;

namespace ZMovie.Api.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/assistant/chat", async (ISender sender, HttpContext context, AssistantChatRequest request, CancellationToken ct) =>
                (await sender.Send(new AskCatalogAssistantQuery(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), request.Message, request.Locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantReply>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        return endpoints;
    }
}

public sealed record AssistantChatRequest(string Message, string? Locale);
