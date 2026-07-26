using MediatR;
using System.Security.Claims;
using ZMovie.Api;
using ZMovie.Application.Assistant;

namespace ZMovie.Api.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/assistant/context", async (ISender sender, HttpContext context, AssistantChatRequest request, CancellationToken ct) =>
                (await sender.Send(new GetAssistantContextQuery(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), request.Message, request.Locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantContextResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapGet("/v1/assistant/context", async (ISender sender, HttpContext context, string? message, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetAssistantContextQuery(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), message ?? string.Empty, locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantContextResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapPost("/v1/assistant/chat", async (ISender sender, HttpContext context, AssistantChatRequest request, CancellationToken ct) =>
                (await sender.Send(new AskCatalogAssistantQuery(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), request.Message, request.Locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantReply>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapGet("/v1/assistant/chat", async (ISender sender, HttpContext context, string? message, string? locale, CancellationToken ct) =>
                (await sender.Send(new AskCatalogAssistantQuery(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), message ?? string.Empty, locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantReply>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapPost("/v1/assistant/feedback", async (ISender sender, HttpContext context, AssistantFeedbackRequest request, CancellationToken ct) =>
                (await sender.Send(new RecordAssistantFeedbackCommand(Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!), request.RecommendationId, request.Slug, request.EventType), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        return endpoints;
    }
}

public sealed record AssistantChatRequest(string Message, string? Locale);
public sealed record AssistantFeedbackRequest(Guid RecommendationId, string Slug, string EventType);
