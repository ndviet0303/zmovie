using ZMovie.Domain.Common;

namespace ZMovie.Domain.Identity;

public sealed class User : AggregateRoot, IEntity<UserId>
{
    private User()
    {
        Email = string.Empty;
        DisplayName = string.Empty;
    }

    private User(
        UserId id,
        ExternalIdentity externalIdentity,
        string email,
        string displayName,
        string? avatarUrl,
        Role role,
        DateTimeOffset occurredAt)
    {
        Id = id;
        ExternalIdentity = externalIdentity;
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Role = role;
        CreatedAt = occurredAt;
        LastSignedInAt = occurredAt;
    }

    public UserId Id { get; private set; }
    public ExternalIdentity ExternalIdentity { get; private set; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Role Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSignedInAt { get; private set; }
    public DateTimeOffset? VipExpiresAt { get; private set; }
    public string SubscriptionTier { get; private set; } = "Free";

    public bool IsVipActive(DateTimeOffset now) => VipExpiresAt.HasValue && VipExpiresAt.Value > now;

    public static User Create(
        UserId id,
        ExternalIdentity externalIdentity,
        string email,
        string displayName,
        string? avatarUrl,
        Role role,
        DateTimeOffset occurredAt)
    {
        var user = new User(id, externalIdentity, email, displayName, avatarUrl, role, occurredAt);
        user.RaiseDomainEvent(new UserCreatedDomainEvent(id, externalIdentity, email, role, occurredAt));
        return user;
    }

    public void RecordSignIn(string email, string displayName, string? avatarUrl, DateTimeOffset occurredAt)
    {
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        LastSignedInAt = occurredAt;

        RaiseDomainEvent(new UserSignedInDomainEvent(Id, occurredAt));
    }

    public void PromoteToAdmin()
    {
        var oldRole = Role;
        Role = Role.Admin;

        if (oldRole != Role.Admin)
        {
            RaiseDomainEvent(new UserRoleChangedDomainEvent(Id, oldRole, Role.Admin, LastSignedInAt));
        }
    }

    public void ChangeRole(Role newRole)
    {
        var oldRole = Role;
        Role = newRole;

        if (oldRole != newRole)
        {
            RaiseDomainEvent(new UserRoleChangedDomainEvent(Id, oldRole, newRole, LastSignedInAt));
        }
    }

    public void ExtendVip(string tier, int months, DateTimeOffset occurredAt)
    {
        SubscriptionTier = tier;
        var baseline = VipExpiresAt.HasValue && VipExpiresAt.Value > occurredAt ? VipExpiresAt.Value : occurredAt;
        VipExpiresAt = baseline.AddMonths(months);
    }
}
