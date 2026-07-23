using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using ZMovie.Application.Identity;

namespace ZMovie.Infrastructure.Identity;

public sealed class GoogleIdentityVerifier(IConfiguration config) : IGoogleIdentityVerifier
{
    public async Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct)
    {
        var clientId = config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(credential)) return null;
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
            if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email) || !payload.EmailVerified) return null;
            return new(payload.Subject, payload.Email, payload.Name ?? payload.Email, payload.Picture);
        }
        catch (InvalidJwtException) { return null; }
    }
}
