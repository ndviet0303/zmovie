namespace ZMovie.Domain.Identity;

public enum LastAdminDecision
{
    Allowed,
    RefusedLastAdmin,
}

public static class LastAdminPolicy
{
    public static LastAdminDecision EvaluateDemotion(Role currentRole, Role newRole, int currentAdminCount)
    {
        if (currentRole.IsAdmin && !newRole.IsAdmin && currentAdminCount <= 1)
        {
            return LastAdminDecision.RefusedLastAdmin;
        }

        return LastAdminDecision.Allowed;
    }
}
