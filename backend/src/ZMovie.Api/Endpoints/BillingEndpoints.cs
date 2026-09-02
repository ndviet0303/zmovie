using MediatR;
using ZMovie.Api;
using ZMovie.Application.Billing;

namespace ZMovie.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/billing/checkout", async (ISender sender, HttpContext context, VipCheckoutRequest request, CancellationToken ct) =>
        {
            var userId = UserIdentityAdapter.GetRequiredUserId(context.User);
            return (await sender.Send(new CreateVipCheckoutCommand(userId, request.Tier, request.Months), ct)).ToApiResult();
        })
            .WithTags("Billing")
            .Produces<VipCheckoutResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors()
            .RequireAuthorization();

        endpoints.MapPost("/v1/billing/webhook", async (ISender sender, BillingWebhookRequest request, CancellationToken ct) =>
        {
            return (await sender.Send(new ProcessBillingWebhookCommand(request.OrderCode, request.Amount, request.SecretKey), ct)).ToApiResult();
        })
            .WithTags("Billing")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        return endpoints;
    }
}

public sealed record VipCheckoutRequest(string Tier, int Months);
public sealed record BillingWebhookRequest(string OrderCode, int Amount, string? SecretKey);
