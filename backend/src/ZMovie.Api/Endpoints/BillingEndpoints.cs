using MediatR;
using ZMovie.Api;
using ZMovie.Application.Billing;
using ZMovie.Application.Identity;


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

        endpoints.MapPost("/v1/webhooks/payos", async (ISender sender, IConfiguration config, PayOsWebhookPayload payload, CancellationToken ct) =>
        {
            var checksumKey = config["Payment:PayOS:ChecksumKey"] ?? config["PAYOS_CHECKSUM_KEY"];
            return (await sender.Send(new ProcessPayOsWebhookCommand(payload, checksumKey), ct)).ToApiResult();
        })
            .WithTags("Billing")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        endpoints.MapPost("/v1/webhooks/sepay", async (ISender sender, IConfiguration config, HttpContext context, SePayWebhookPayload payload, CancellationToken ct) =>
        {
            var expectedKey = config["Payment:SePay:ApiKey"] ?? config["SEPAY_API_KEY"];
            var authHeader = context.Request.Headers.Authorization.ToString();
            return (await sender.Send(new ProcessSePayWebhookCommand(payload, authHeader, expectedKey), ct)).ToApiResult();
        })
            .WithTags("Billing")
            .Produces<bool>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        return endpoints;
    }
}


public sealed record VipCheckoutRequest(string Tier, int Months);
public sealed record BillingWebhookRequest(string OrderCode, int Amount, string? SecretKey);
