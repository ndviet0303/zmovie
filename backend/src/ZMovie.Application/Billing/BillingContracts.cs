using ErrorOr;
using MediatR;
using ZMovie.Application.Common;

namespace ZMovie.Application.Billing;

public sealed record VipPlanOption(string Tier, string Name, int Months, int PriceVnd, string Description);

public sealed record VipCheckoutResponse(
    string OrderCode,
    string Tier,
    int Amount,
    string QrCodeUrl,
    string AccountNumber,
    string AccountName,
    string BankName,
    string Description,
    DateTimeOffset ExpiresAt);

public sealed record CreateVipCheckoutCommand(Guid UserId, string Tier, int Months) : ICommand<VipCheckoutResponse>;

public sealed class CreateVipCheckoutHandler : IRequestHandler<CreateVipCheckoutCommand, ErrorOr<VipCheckoutResponse>>
{
    public Task<ErrorOr<VipCheckoutResponse>> Handle(CreateVipCheckoutCommand request, CancellationToken ct)
    {
        var randomSuffix = Random.Shared.Next(10000, 99999);
        var orderCode = $"ZM_VIP_{randomSuffix}";
        var amount = request.Tier.Equals("Yearly", StringComparison.OrdinalIgnoreCase) ? 599000 : 59000;
        var description = orderCode;

        // Standard VietQR QuickLink (Napas 247) format
        // https://img.vietqr.io/image/<BANK_ID>-<ACCOUNT_NO>-<TEMPLATE>.png?amount=<AMOUNT>&addInfo=<DESCRIPTION>&accountName=<ACCOUNT_NAME>
        var qrCodeUrl = $"https://img.vietqr.io/image/970422-0988888888-compact2.png?amount={amount}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString("CONG TY ZMOVIE VIETNAM")}";

        var response = new VipCheckoutResponse(
            OrderCode: orderCode,
            Tier: request.Tier,
            Amount: amount,
            QrCodeUrl: qrCodeUrl,
            AccountNumber: "0988888888",
            AccountName: "CONG TY ZMOVIE VIETNAM",
            BankName: "MBBank (Ngân hàng Quân Đội)",
            Description: description,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15));

        return Task.FromResult<ErrorOr<VipCheckoutResponse>>(response);
    }
}

public sealed record ProcessBillingWebhookCommand(string OrderCode, int Amount, string? SecretKey) : ICommand<bool>;

public sealed class ProcessBillingWebhookHandler : IRequestHandler<ProcessBillingWebhookCommand, ErrorOr<bool>>
{
    public Task<ErrorOr<bool>> Handle(ProcessBillingWebhookCommand request, CancellationToken ct)
    {
        return Task.FromResult<ErrorOr<bool>>(true);
    }
}
