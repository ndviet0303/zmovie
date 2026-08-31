using System.Reflection;
using ZMovie.Domain.Identity;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Identity;

public sealed class UserTests
{
    private static readonly UserId Id = new(Guid.Parse("67d06bf3-913b-4a55-b6ac-e758ecf9d0d7"));
    private static readonly ExternalIdentity GoogleId = new("google-sub-123");

    [Fact]
    public void Create_sets_identity_and_same_explicit_timestamps()
    {
        var occurredAt = new DateTimeOffset(2026, 8, 31, 8, 15, 0, TimeSpan.Zero);

        var user = User.Create(Id, GoogleId, "user@test.com", "User Name", "https://avatar.test/pic.png", Role.Member, occurredAt);

        Assert.Equal(Id, user.Id);
        Assert.Equal(GoogleId, user.ExternalIdentity);
        Assert.Equal("user@test.com", user.Email);
        Assert.Equal("User Name", user.DisplayName);
        Assert.Equal("https://avatar.test/pic.png", user.AvatarUrl);
        Assert.Equal(Role.Member, user.Role);
        Assert.Equal(occurredAt, user.CreatedAt);
        Assert.Equal(occurredAt, user.LastSignedInAt);
    }

    [Fact]
    public void RecordSignIn_updates_profile_and_last_signed_in_at()
    {
        var createdAt = new DateTimeOffset(2026, 8, 30, 8, 15, 0, TimeSpan.Zero);
        var signedInAt = createdAt.AddDays(1);
        var user = User.Create(Id, GoogleId, "old@test.com", "Old Name", null, Role.Member, createdAt);

        user.RecordSignIn("new@test.com", "New Name", "https://avatar.test/new.png", signedInAt);

        Assert.Equal("new@test.com", user.Email);
        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal("https://avatar.test/new.png", user.AvatarUrl);
        Assert.Equal(createdAt, user.CreatedAt);
        Assert.Equal(signedInAt, user.LastSignedInAt);
    }

    [Fact]
    public void PromoteToAdmin_changes_role_to_admin()
    {
        var user = User.Create(Id, GoogleId, "user@test.com", "User", null, Role.Member, DateTimeOffset.UnixEpoch);

        user.PromoteToAdmin();

        Assert.Equal(Role.Admin, user.Role);
    }

    [Fact]
    public void ChangeRole_changes_role()
    {
        var user = User.Create(Id, GoogleId, "user@test.com", "User", null, Role.Admin, DateTimeOffset.UnixEpoch);

        user.ChangeRole(Role.Member);

        Assert.Equal(Role.Member, user.Role);
    }

    [Fact]
    public void Aggregate_has_only_a_private_parameterless_constructor_for_materialization()
    {
        var constructor = typeof(User).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
        Assert.Empty(typeof(User).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void Aggregate_state_can_only_be_changed_through_domain_behavior()
    {
        var stateProperties = typeof(User).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.NotEmpty(stateProperties);
        Assert.All(stateProperties, property =>
        {
            Assert.NotNull(property.SetMethod);
            Assert.True(property.SetMethod.IsPrivate);
        });
    }
}
