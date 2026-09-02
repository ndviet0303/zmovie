using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ErrorOr;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Identity;

namespace ZMovie.Application.Identity;

public interface IVipSubscriptionRepository
{
    Task<VipSubscription?> FindByGatewayReferenceAsync(string gateway, string reference, CancellationToken ct);
    Task AddAsync(VipSubscription subscription, CancellationToken ct);
    Task<IReadOnlyList<VipSubscription>> ListByUserIdAsync(UserId userId, CancellationToken ct);
}

public sealed record PayOsWebhookData(
    long OrderCode,
    long Amount,
    string Description,
    string? AccountNumber,
    string? Reference,
    string? TransactionDateTime,
    string? Currency,
    string? PaymentLinkId,
    string? Code,
    string? Desc);

public sealed record PayOsWebhookPayload(
    string Code,
    string Desc,
    bool Success,
    PayOsWebhookData? Data,
    string? Signature);

public sealed record ProcessPayOsWebhookCommand(
    PayOsWebhookPayload Payload,
    string? ChecksumKey) : ICommand<bool>;

public sealed record SePayWebhookPayload(
    long Id,
    string? Gateway,
    string? TransactionDate,
    string? AccountNumber,
    string? SubAccount,
    decimal AmountIn,
    decimal AmountOut,
    decimal Accumulated,
    string? Code,
    string? TransactionContent,
    string? ReferenceNumber,
    string? Body);

public sealed record ProcessSePayWebhookCommand(
    SePayWebhookPayload Payload,
    string? AuthorizationHeader,
    string? ExpectedApiKey) : ICommand<bool>;

public static class VipWebhookSecurity
{
    public static bool VerifyPayOsSignature(PayOsWebhookData data, string signature, string checksumKey)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(checksumKey))
        {
            return false;
        }

        // PayOS sorted signature format: amount=X&cancelUrl=Y...
        var sortedPairs = new SortedDictionary<string, string>
        {
            ["amount"] = data.Amount.ToString(),
            ["cancelUrl"] = "",
            ["description"] = data.Description ?? "",
            ["orderCode"] = data.OrderCode.ToString(),
            ["returnUrl"] = ""
        };

        var rawData = string.Join("&", sortedPairs.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={p.Value}"));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var computedSignature = Convert.ToHexStringLower(hash);

        return string.Equals(computedSignature, signature.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ProcessPayOsWebhookHandler(
    IVipSubscriptionRepository vipRepo,
    IUserRepository userRepo,
    TimeProvider timeProvider) : IRequestHandler<ProcessPayOsWebhookCommand, ErrorOr<bool>>
{
    private static readonly Regex OrderPattern = new(@"ZMVIP[\s_-]*([a-f0-9\-]{36})[\s_-]*([a-zA-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<ErrorOr<bool>> Handle(ProcessPayOsWebhookCommand request, CancellationToken ct)
    {
        var data = request.Payload.Data;
        if (data == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.ChecksumKey) && !string.IsNullOrWhiteSpace(request.Payload.Signature))
        {
            if (!VipWebhookSecurity.VerifyPayOsSignature(data, request.Payload.Signature, request.ChecksumKey))
            {
                return Error.Unauthorized("payos.signature.invalid", "Chữ ký webhook PayOS không hợp lệ.");
            }
        }

        var reference = string.IsNullOrWhiteSpace(data.Reference) ? data.OrderCode.ToString() : data.Reference;

        // Idempotency guard: if already processed, return immediately
        var existing = await vipRepo.FindByGatewayReferenceAsync("PayOS", reference, ct);
        if (existing != null)
        {
            return true;
        }

        var match = OrderPattern.Match(data.Description ?? "");
        if (!match.Success || !Guid.TryParse(match.Groups[1].Value, out var parsedGuid))
        {
            // If description doesn't match pattern, return success to acknowledge webhook without upgrade
            return true;
        }

        var planCode = match.Groups[2].Value;
        var plan = VipPlan.FromCode(planCode) ?? VipPlan.Monthly;

        var user = await userRepo.FindByIdAsync(new UserId(parsedGuid), ct);
        if (user == null)
        {
            return true;
        }

        var now = timeProvider.GetUtcNow();
        user.ExtendVip(plan.Code, plan.DurationMonths, now);
        await userRepo.SaveChangesAsync(ct);

        var subscription = VipSubscription.Create(
            VipSubscriptionId.New(),
            user.Id,
            plan.Code,
            data.Amount,
            "PayOS",
            reference,
            now,
            user.VipExpiresAt ?? now.AddMonths(plan.DurationMonths));

        await vipRepo.AddAsync(subscription, ct);

        return true;
    }
}

public sealed class ProcessSePayWebhookHandler(
    IVipSubscriptionRepository vipRepo,
    IUserRepository userRepo,
    TimeProvider timeProvider) : IRequestHandler<ProcessSePayWebhookCommand, ErrorOr<bool>>
{
    private static readonly Regex ContentPattern = new(@"ZMVIP[\s_-]*([a-f0-9\-]{36})[\s_-]*([a-zA-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<ErrorOr<bool>> Handle(ProcessSePayWebhookCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.ExpectedApiKey))
        {
            var auth = request.AuthorizationHeader ?? string.Empty;
            if (!auth.Contains(request.ExpectedApiKey, StringComparison.OrdinalIgnoreCase))
            {
                return Error.Unauthorized("sepay.auth.invalid", "Mã xác thực SePay không hợp lệ.");
            }
        }

        var reference = request.Payload.Id.ToString();
        var existing = await vipRepo.FindByGatewayReferenceAsync("SePay", reference, ct);
        if (existing != null)
        {
            return true;
        }

        var match = ContentPattern.Match(request.Payload.TransactionContent ?? "");
        if (!match.Success || !Guid.TryParse(match.Groups[1].Value, out var parsedGuid))
        {
            return true;
        }

        var planCode = match.Groups[2].Value;
        var plan = VipPlan.FromCode(planCode) ?? VipPlan.Monthly;

        var user = await userRepo.FindByIdAsync(new UserId(parsedGuid), ct);
        if (user == null)
        {
            return true;
        }

        var now = timeProvider.GetUtcNow();
        user.ExtendVip(plan.Code, plan.DurationMonths, now);
        await userRepo.SaveChangesAsync(ct);

        var subscription = VipSubscription.Create(
            VipSubscriptionId.New(),
            user.Id,
            plan.Code,
            request.Payload.AmountIn,
            "SePay",
            reference,
            now,
            user.VipExpiresAt ?? now.AddMonths(plan.DurationMonths));

        await vipRepo.AddAsync(subscription, ct);

        return true;
    }
}
