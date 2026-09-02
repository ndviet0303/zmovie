using System.Security.Claims;
using MediatR;
using ZMovie.Api;
using ZMovie.Application.Assistant;
using ZMovie.Application.Personalization;

namespace ZMovie.Api.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/assistant/context", async (ISender sender, HttpContext context, AssistantChatRequest request, CancellationToken ct) =>
                (await sender.Send(new GetAssistantContextQuery(UserIdentityAdapter.GetRequiredUserId(context.User), request.Message, request.Locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantContextResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapGet("/v1/assistant/context", async (ISender sender, HttpContext context, string? message, string? locale, CancellationToken ct) =>
                (await sender.Send(new GetAssistantContextQuery(UserIdentityAdapter.GetRequiredUserId(context.User), message ?? string.Empty, locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantContextResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapPost("/v1/assistant/chat", async (ISender sender, HttpContext context, AssistantChatRequest request, CancellationToken ct) =>
                (await sender.Send(new AskCatalogAssistantQuery(UserIdentityAdapter.GetRequiredUserId(context.User), request.Message, request.Locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantReply>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapGet("/v1/assistant/chat", async (ISender sender, HttpContext context, string? message, string? locale, CancellationToken ct) =>
                (await sender.Send(new AskCatalogAssistantQuery(UserIdentityAdapter.GetRequiredUserId(context.User), message ?? string.Empty, locale), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<AssistantReply>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        endpoints.MapGet("/v1/assistant/chat/stream", async (
            ISender sender,
            HttpContext context,
            string? message,
            string? locale,
            CancellationToken ct) =>
        {
            var userId = UserIdentityAdapter.GetRequiredUserId(context.User);
            var query = new AskCatalogAssistantQuery(userId, message ?? string.Empty, locale);
            var replyOrError = await sender.Send(query, ct);

            if (replyOrError.IsError)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var reply = replyOrError.Value;
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";

            var contextData = System.Text.Json.JsonSerializer.Serialize(new { suggestions = reply.Suggestions, recommendationId = reply.RecommendationId });
            await context.Response.WriteAsync($"event: context\ndata: {contextData}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);

            var words = reply.Message.Split(' ');
            for (var i = 0; i < words.Length; i++)
            {
                if (ct.IsCancellationRequested) break;
                var token = (i == 0 ? string.Empty : " ") + words[i];
                var tokenData = System.Text.Json.JsonSerializer.Serialize(new { token });
                await context.Response.WriteAsync($"event: token\ndata: {tokenData}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
                await Task.Delay(10, ct);
            }

            await context.Response.WriteAsync("event: done\ndata: [DONE]\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        })
            .WithTags("Assistant")
            .RequireAuthorization();
        endpoints.MapPost("/v1/assistant/feedback", async (ISender sender, HttpContext context, AssistantFeedbackRequest request, CancellationToken ct) =>
                (await sender.Send(new RecordPersonalizationFeedbackCommand(UserIdentityAdapter.GetRequiredUserId(context.User), request.RecommendationId, request.Slug, request.EventType), ct)).ToApiResult())
            .WithTags("Assistant")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();
        return endpoints;
    }
}

public sealed record AssistantChatRequest(string Message, string? Locale);
public sealed record AssistantFeedbackRequest(Guid RecommendationId, string Slug, string EventType);
