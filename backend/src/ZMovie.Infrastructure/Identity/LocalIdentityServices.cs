using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;

namespace ZMovie.Infrastructure.Identity;

public sealed class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string passwordHash, string password, out bool needsRehash)
    {
        var result = _hasher.VerifyHashedPassword(user, passwordHash, password);
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }
}

public sealed class SmtpIdentityEmailSender(
    IConfiguration configuration,
    ILogger<SmtpIdentityEmailSender> logger) : IIdentityEmailSender
{
    public async Task SendPasswordResetAsync(string email, string displayName, string token, CancellationToken ct)
    {
        var resetBaseUrl = configuration["Identity:PasswordResetUrl"] ?? "http://localhost:3000/reset-password";
        var resetUrl = $"{resetBaseUrl}?login={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP is not configured. Password reset URL for {Email}: {ResetUrl}", email, resetUrl);
            return;
        }

        using var message = new MailMessage(
            configuration["Smtp:From"] ?? "no-reply@zmovie.local",
            email,
            "Khôi phục mật khẩu ZMovie",
            $"Xin chào {displayName},\n\nMở liên kết sau trong 30 phút để đặt lại mật khẩu:\n{resetUrl}");
        using var client = new SmtpClient(host, int.TryParse(configuration["Smtp:Port"], out var port) ? port : 587)
        {
            EnableSsl = !string.Equals(configuration["Smtp:UseSsl"], "false", StringComparison.OrdinalIgnoreCase),
        };
        var username = configuration["Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, configuration["Smtp:Password"]);
        await client.SendMailAsync(message, ct);
    }
}
