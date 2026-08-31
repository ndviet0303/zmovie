namespace ZMovie.Domain.Identity;

public sealed class User
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

    public static User Create(
        UserId id,
        ExternalIdentity externalIdentity,
        string email,
        string displayName,
        string? avatarUrl,
        Role role,
        DateTimeOffset occurredAt) =>
        new(id, externalIdentity, email, displayName, avatarUrl, role, occurredAt);

    public void RecordSignIn(string email, string displayName, string? avatarUrl, DateTimeOffset occurredAt)
    {
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        LastSignedInAt = occurredAt;
    }

    public void PromoteToAdmin()
    {
        Role = Role.Admin;
    }

    public void ChangeRole(Role newRole)
    {
        Role = newRole;
    }
}
