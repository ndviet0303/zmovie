using ZMovie.Domain.Identity;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Identity;

public sealed class RoleTests
{
    [Theory]
    [InlineData("member", "member", false)]
    [InlineData("admin", "admin", true)]
    [InlineData("  ADMIN  ", "admin", true)]
    [InlineData("  Member  ", "member", false)]
    public void TryCreate_parses_valid_roles_ignoring_case_and_whitespace(string input, string expectedValue, bool expectedIsAdmin)
    {
        var result = Role.TryCreate(input, out var role);

        Assert.True(result);
        Assert.Equal(expectedValue, role.Value);
        Assert.Equal(expectedIsAdmin, role.IsAdmin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("superuser")]
    [InlineData("root")]
    public void TryCreate_rejects_unknown_roles(string? input)
    {
        var result = Role.TryCreate(input, out var role);

        Assert.False(result);
        Assert.Equal(default, role);
    }

    [Theory]
    [InlineData("admin", "admin")]
    [InlineData("member", "member")]
    [InlineData("invalid", "member")]
    [InlineData(null, "member")]
    public void Normalize_falls_back_to_member_for_unknown_values(string? input, string expected)
    {
        var role = Role.Normalize(input);

        Assert.Equal(expected, role.Value);
    }

    [Theory]
    [InlineData("admin", true)]
    [InlineData("member", true)]
    [InlineData("ADMIN", true)]
    [InlineData("other", false)]
    [InlineData(null, false)]
    public void IsKnown_checks_validity(string? input, bool expected)
    {
        Assert.Equal(expected, Role.IsKnown(input));
    }
}
