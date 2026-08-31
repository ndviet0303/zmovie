using ZMovie.Domain.Identity;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Identity;

public sealed class LastAdminPolicyTests
{
    [Fact]
    public void EvaluateDemotion_refuses_demoting_the_last_remaining_admin()
    {
        var decision = LastAdminPolicy.EvaluateDemotion(Role.Admin, Role.Member, 1);

        Assert.Equal(LastAdminDecision.RefusedLastAdmin, decision);
    }

    [Fact]
    public void EvaluateDemotion_refuses_demoting_when_zero_admins_counted()
    {
        var decision = LastAdminPolicy.EvaluateDemotion(Role.Admin, Role.Member, 0);

        Assert.Equal(LastAdminDecision.RefusedLastAdmin, decision);
    }

    [Fact]
    public void EvaluateDemotion_allows_demoting_when_multiple_admins_exist()
    {
        var decision = LastAdminPolicy.EvaluateDemotion(Role.Admin, Role.Member, 2);

        Assert.Equal(LastAdminDecision.Allowed, decision);
    }

    [Fact]
    public void EvaluateDemotion_allows_re_assigning_admin_to_admin_even_if_sole_admin()
    {
        var decision = LastAdminPolicy.EvaluateDemotion(Role.Admin, Role.Admin, 1);

        Assert.Equal(LastAdminDecision.Allowed, decision);
    }

    [Fact]
    public void EvaluateDemotion_allows_member_to_member_or_member_to_admin()
    {
        Assert.Equal(LastAdminDecision.Allowed, LastAdminPolicy.EvaluateDemotion(Role.Member, Role.Member, 1));
        Assert.Equal(LastAdminDecision.Allowed, LastAdminPolicy.EvaluateDemotion(Role.Member, Role.Admin, 1));
    }
}
