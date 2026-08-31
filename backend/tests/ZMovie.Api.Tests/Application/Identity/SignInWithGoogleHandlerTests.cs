using FluentAssertions;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using Xunit;

namespace ZMovie.Api.Tests.Application.Identity;

public sealed class SignInWithGoogleHandlerTests
{
    private static readonly DateTimeOffset InitialTime = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_creates_new_user_when_not_found()
    {
        var verifier = new FakeVerifier(new GoogleIdentity("sub-1", "user@test.com", "User Name", "https://pic/1.jpg"));
        var repo = new FakeUserRepository();
        var allowlist = new FakeAllowlist();
        var time = new FixedTimeProvider(InitialTime);
        var handler = new SignInWithGoogleHandler(verifier, repo, allowlist, time);

        var result = await handler.Handle(new SignInWithGoogleCommand("valid-credential"), default);

        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("user@test.com");
        result.Value.DisplayName.Should().Be("User Name");
        result.Value.AvatarUrl.Should().Be("https://pic/1.jpg");
        result.Value.Role.Should().Be(Role.MemberName);

        repo.StoredUser.Should().NotBeNull();
        repo.StoredUser!.ExternalIdentity.Should().Be(new ExternalIdentity("sub-1"));
        repo.StoredUser.CreatedAt.Should().Be(InitialTime);
        repo.StoredUser.LastSignedInAt.Should().Be(InitialTime);
    }

    [Fact]
    public async Task Handle_promotes_new_user_if_email_is_allowlisted()
    {
        var verifier = new FakeVerifier(new GoogleIdentity("sub-admin", "admin@test.com", "Admin", null));
        var repo = new FakeUserRepository();
        var allowlist = new FakeAllowlist("admin@test.com");
        var time = new FixedTimeProvider(InitialTime);
        var handler = new SignInWithGoogleHandler(verifier, repo, allowlist, time);

        var result = await handler.Handle(new SignInWithGoogleCommand("valid-credential"), default);

        result.IsError.Should().BeFalse();
        result.Value.Role.Should().Be(Role.AdminName);
        repo.StoredUser!.Role.Should().Be(Role.Admin);
    }

    [Fact]
    public async Task Handle_updates_profile_and_promotes_existing_member_if_allowlisted()
    {
        var existing = User.Create(UserId.New(), new ExternalIdentity("sub-1"), "old@test.com", "Old", null, Role.Member, InitialTime);
        var repo = new FakeUserRepository(existing);
        var verifier = new FakeVerifier(new GoogleIdentity("sub-1", "promoted@test.com", "New Name", "https://new/pic.jpg"));
        var allowlist = new FakeAllowlist("promoted@test.com");
        var newTime = InitialTime.AddDays(2);
        var time = new FixedTimeProvider(newTime);
        var handler = new SignInWithGoogleHandler(verifier, repo, allowlist, time);

        var result = await handler.Handle(new SignInWithGoogleCommand("valid-credential"), default);

        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("promoted@test.com");
        result.Value.DisplayName.Should().Be("New Name");
        result.Value.AvatarUrl.Should().Be("https://new/pic.jpg");
        result.Value.Role.Should().Be(Role.AdminName);

        existing.Role.Should().Be(Role.Admin);
        existing.CreatedAt.Should().Be(InitialTime);
        existing.LastSignedInAt.Should().Be(newTime);
    }

    [Fact]
    public async Task Handle_does_not_demote_admin_when_not_in_allowlist()
    {
        var existing = User.Create(UserId.New(), new ExternalIdentity("sub-1"), "admin@test.com", "Admin", null, Role.Admin, InitialTime);
        var repo = new FakeUserRepository(existing);
        var verifier = new FakeVerifier(new GoogleIdentity("sub-1", "admin@test.com", "Admin", null));
        var allowlist = new FakeAllowlist(); // empty
        var time = new FixedTimeProvider(InitialTime.AddDays(1));
        var handler = new SignInWithGoogleHandler(verifier, repo, allowlist, time);

        var result = await handler.Handle(new SignInWithGoogleCommand("valid-credential"), default);

        result.IsError.Should().BeFalse();
        result.Value.Role.Should().Be(Role.AdminName);
        existing.Role.Should().Be(Role.Admin);
    }

    [Fact]
    public async Task Handle_returns_unauthorized_for_invalid_credential()
    {
        var verifier = new FakeVerifier(null);
        var repo = new FakeUserRepository();
        var allowlist = new FakeAllowlist();
        var time = new FixedTimeProvider(InitialTime);
        var handler = new SignInWithGoogleHandler(verifier, repo, allowlist, time);

        var result = await handler.Handle(new SignInWithGoogleCommand("bad-token"), default);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("auth.google.invalid_credential");
    }

    private sealed class FakeVerifier(GoogleIdentity? identity) : IGoogleIdentityVerifier
    {
        public Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct) => Task.FromResult(identity);
    }

    private sealed class FakeUserRepository(User? initial = null) : IUserRepository
    {
        public User? StoredUser { get; set; } = initial;
        public int SaveChangesCalls { get; private set; }

        public Task<User?> FindByIdAsync(UserId id, CancellationToken ct) =>
            Task.FromResult(StoredUser?.Id == id ? StoredUser : null);

        public Task<User?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken ct) =>
            Task.FromResult(StoredUser?.ExternalIdentity == externalIdentity ? StoredUser : null);

        public void Add(User user) => StoredUser = user;

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }

        public Task<SetRoleOutcome> ChangeRoleWithLastAdminGuardAsync(UserId userId, Role newRole, bool guardLastAdmin, CancellationToken ct) =>
            Task.FromResult(SetRoleOutcome.Updated);
    }

    private sealed class FakeAllowlist(params string[] emails) : IAdminAllowlist
    {
        public bool IsAllowlisted(string email) => emails.Contains(email);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
