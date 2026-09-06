using FluentAssertions;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure.Identity;
using Xunit;

namespace ZMovie.Api.Tests.Application.Identity;

public sealed class LocalIdentityHandlerTests
{
    [Fact]
    public async Task Registration_login_and_password_reset_complete_the_local_account_flow()
    {
        var repository = new FakeUserRepository();
        var passwords = new IdentityPasswordService();
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero));
        var registration = await new RegisterLocalUserHandler(repository, passwords, new FakeAllowlist(), time)
            .Handle(new("movie_fan", "Movie Fan", "fan@example.test", "initial-password"), default);

        registration.IsError.Should().BeFalse();
        repository.StoredUser!.Username.Should().Be("movie_fan");
        (await new SignInWithPasswordHandler(repository, passwords, time)
            .Handle(new("movie_fan", "wrong-password"), default)).FirstError.Code
            .Should().Be("auth.local.invalid_credentials");
        (await new SignInWithPasswordHandler(repository, passwords, time)
            .Handle(new("fan@example.test", "initial-password"), default)).IsError
            .Should().BeFalse();

        var email = new FakeEmailSender();
        (await new RequestPasswordResetHandler(repository, email, time)
            .Handle(new("fan@example.test"), default)).Value.Should().BeTrue();
        email.Token.Should().NotBeNullOrWhiteSpace();
        (await new ResetPasswordHandler(repository, passwords, time)
            .Handle(new("movie_fan", email.Token!, "replacement-password"), default)).Value
            .Should().BeTrue();
        (await new SignInWithPasswordHandler(repository, passwords, time)
            .Handle(new("movie_fan", "replacement-password"), default)).IsError
            .Should().BeFalse();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? StoredUser { get; private set; }

        public Task<User?> FindByIdAsync(UserId id, CancellationToken ct) =>
            Task.FromResult(StoredUser?.Id == id ? StoredUser : null);

        public Task<User?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken ct) =>
            Task.FromResult(StoredUser?.ExternalIdentity == externalIdentity ? StoredUser : null);

        public Task<User?> FindByLoginAsync(string login, CancellationToken ct)
        {
            var normalized = login.Trim().ToLowerInvariant();
            return Task.FromResult(
                StoredUser is not null &&
                (StoredUser.Username == normalized || StoredUser.Email == normalized)
                    ? StoredUser
                    : null);
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(StoredUser?.Email == email.Trim().ToLowerInvariant() ? StoredUser : null);

        public void Add(User user) => StoredUser = user;
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<SetRoleOutcome> ChangeRoleWithLastAdminGuardAsync(UserId userId, Role newRole, bool guardLastAdmin, CancellationToken ct) =>
            Task.FromResult(SetRoleOutcome.Updated);
    }

    private sealed class FakeEmailSender : IIdentityEmailSender
    {
        public string? Token { get; private set; }
        public Task SendPasswordResetAsync(string email, string displayName, string token, CancellationToken ct)
        {
            Token = token;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAllowlist : IAdminAllowlist
    {
        public bool IsAllowlisted(string email) => false;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
