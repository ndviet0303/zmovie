using System.Security.Cryptography;
using System.Text;

namespace ZMovie.Infrastructure.Operations;

public static class AntiLeechProtection
{
    public static string GenerateStreamingToken(string streamPath, TimeSpan validFor, string secret, DateTimeOffset now)
    {
        var exp = now.Add(validFor).ToUnixTimeSeconds();
        var raw = $"{streamPath.Trim().ToLowerInvariant()}|{exp}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        var sig = Convert.ToHexStringLower(hash);

        return $"exp={exp}&sig={sig}";
    }

    public static bool ValidateStreamingToken(string streamPath, long exp, string sig, string secret, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(sig) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        if (now.ToUnixTimeSeconds() > exp)
        {
            return false; // Token expired
        }

        var raw = $"{streamPath.Trim().ToLowerInvariant()}|{exp}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        var expectedSig = Convert.ToHexStringLower(expectedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSig),
            Encoding.UTF8.GetBytes(sig.Trim().ToLowerInvariant()));
    }
}
