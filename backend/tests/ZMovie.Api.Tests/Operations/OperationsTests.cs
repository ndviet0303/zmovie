using FluentAssertions;
using Xunit;
using ZMovie.Infrastructure.Operations;

namespace ZMovie.Api.Tests.Operations;

public sealed class OperationsTests
{
    private static readonly DateTimeOffset InitialTime = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
    private const string Secret = "super_secret_anti_leech_key_2026";

    [Fact]
    public void GenerateStreamingToken_and_Validate_succeeds_for_valid_unexpired_token()
    {
        var tokenQuery = AntiLeechProtection.GenerateStreamingToken(
            "/stream/r2/natra-2/ep1.m3u8",
            TimeSpan.FromHours(2),
            Secret,
            InitialTime);

        // Parse query params exp & sig
        var parts = tokenQuery.Split('&');
        var exp = long.Parse(parts[0].Replace("exp=", ""));
        var sig = parts[1].Replace("sig=", "");

        var isValid = AntiLeechProtection.ValidateStreamingToken(
            "/stream/r2/natra-2/ep1.m3u8",
            exp,
            sig,
            Secret,
            InitialTime.AddMinutes(30));

        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateStreamingToken_fails_when_token_is_expired()
    {
        var tokenQuery = AntiLeechProtection.GenerateStreamingToken(
            "/stream/r2/natra-2/ep1.m3u8",
            TimeSpan.FromHours(1),
            Secret,
            InitialTime);

        var parts = tokenQuery.Split('&');
        var exp = long.Parse(parts[0].Replace("exp=", ""));
        var sig = parts[1].Replace("sig=", "");

        // Checked 2 hours later (after 1 hour expiration)
        var isValid = AntiLeechProtection.ValidateStreamingToken(
            "/stream/r2/natra-2/ep1.m3u8",
            exp,
            sig,
            Secret,
            InitialTime.AddHours(2));

        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateStreamingToken_fails_when_signature_is_tampered()
    {
        var tokenQuery = AntiLeechProtection.GenerateStreamingToken(
            "/stream/r2/natra-2/ep1.m3u8",
            TimeSpan.FromHours(2),
            Secret,
            InitialTime);

        var parts = tokenQuery.Split('&');
        var exp = long.Parse(parts[0].Replace("exp=", ""));

        var isValid = AntiLeechProtection.ValidateStreamingToken(
            "/stream/r2/natra-2/ep1.m3u8",
            exp,
            "tampered_signature_hex_12345",
            Secret,
            InitialTime.AddMinutes(10));

        isValid.Should().BeFalse();
    }
}
