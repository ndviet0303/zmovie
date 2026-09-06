using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;
using ZMovie.Domain.Identity;

namespace ZMovie.Application.Identity;

public sealed record GoogleIdentity(string Subject, string Email, string DisplayName, string? AvatarUrl);
public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName, string? AvatarUrl, string Role);

public interface IGoogleIdentityVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct);
}

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string passwordHash, string password, out bool needsRehash);
}

public interface IIdentityEmailSender
{
    Task SendPasswordResetAsync(string email, string displayName, string token, CancellationToken ct);
}

public interface IAdminAllowlist
{
    bool IsAllowlisted(string email);
}

public enum SetRoleOutcome
{
    Updated,
    NotFound,
    LastAdmin,
}

public interface IUserRepository
{
    Task<User?> FindByIdAsync(UserId id, CancellationToken ct);
    Task<User?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken ct);
    void Add(User user);
    Task SaveChangesAsync(CancellationToken ct);
    Task<User?> FindByLoginAsync(string login, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<SetRoleOutcome> ChangeRoleWithLastAdminGuardAsync(UserId userId, Role newRole, bool guardLastAdmin, CancellationToken ct);
}

public sealed record UserSummary(Guid Id, string Email, string DisplayName, string? AvatarUrl, string Role, DateTimeOffset CreatedAt, DateTimeOffset LastSignedInAt);

public interface IUserQueries
{
    Task<IReadOnlyList<UserSummary>> ListUsersAsync(string? query, string? role, int page, int pageSize, CancellationToken ct);
    Task<UserSummary?> GetUserAsync(UserId userId, CancellationToken ct);
    Task<int> CountAdminsAsync(CancellationToken ct);
}

public sealed record SignInWithGoogleCommand(string Credential) : ICommand<AuthenticatedUser>;

public sealed class SignInWithGoogleValidator : AbstractValidator<SignInWithGoogleCommand>
{
    // A Google ID token is well under 4 KB; the endpoint is anonymous so the bound
    // credential must be length-capped before it reaches the JWT validator.
    public SignInWithGoogleValidator() => RuleFor(x => x.Credential).NotEmpty().MaximumLength(8_192);
}

public sealed class SignInWithGoogleHandler(
    IGoogleIdentityVerifier verifier,
    IUserRepository users,
    IAdminAllowlist allowlist,
    TimeProvider timeProvider) : IRequestHandler<SignInWithGoogleCommand, ErrorOr<AuthenticatedUser>>
{
    public async Task<ErrorOr<AuthenticatedUser>> Handle(SignInWithGoogleCommand request, CancellationToken ct)
    {
        var identity = await verifier.VerifyAsync(request.Credential, ct);
        if (identity is null)
        {
            return Error.Unauthorized("auth.google.invalid_credential", "Google credential is invalid or expired.");
        }

        var externalIdentity = new ExternalIdentity(identity.Subject);
        var isAllowlistedAdmin = allowlist.IsAllowlisted(identity.Email);
        var now = timeProvider.GetUtcNow();

        var user = await users.FindByExternalIdentityAsync(externalIdentity, ct);
        if (user is null)
        {
            var initialRole = isAllowlistedAdmin ? Role.Admin : Role.Member;
            var googleUsername = $"google-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.Subject)))[..16].ToLowerInvariant()}";
            user = User.Create(UserId.New(), externalIdentity, identity.Email, identity.DisplayName, identity.AvatarUrl, initialRole, now, googleUsername);
            users.Add(user);
        }
        else
        {
            user.RecordSignIn(identity.Email, identity.DisplayName, identity.AvatarUrl, now);
            if (isAllowlistedAdmin && !user.Role.IsAdmin)
            {
                user.PromoteToAdmin();
            }
        }

        try
        {
            await users.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            user = await users.FindByExternalIdentityAsync(externalIdentity, ct);
            if (user is null) throw;
        }

        return new AuthenticatedUser(user.Id.Value, user.Email, user.DisplayName, user.AvatarUrl, user.Role.Value);
    }
}

public sealed record RegisterLocalUserCommand(string Username, string DisplayName, string Email, string Password) : ICommand<AuthenticatedUser>;
public sealed class RegisterLocalUserValidator : AbstractValidator<RegisterLocalUserCommand>
{
    public RegisterLocalUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Matches("^[a-zA-Z0-9_.-]+$").Length(3, 64);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().Length(8, 128);
    }
}
public sealed class RegisterLocalUserHandler(
    IUserRepository users,
    IPasswordService passwords,
    IAdminAllowlist allowlist,
    TimeProvider timeProvider) : IRequestHandler<RegisterLocalUserCommand, ErrorOr<AuthenticatedUser>>
{
    public async Task<ErrorOr<AuthenticatedUser>> Handle(RegisterLocalUserCommand request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByLoginAsync(username, ct) is not null ||
            await users.FindByEmailAsync(email, ct) is not null)
            return Error.Conflict("auth.local.account_exists", "Username or email is already registered.");

        var now = timeProvider.GetUtcNow();
        var role = allowlist.IsAllowlisted(email) ? Role.Admin : Role.Member;
        var user = User.Create(
            UserId.New(),
            new ExternalIdentity($"local:{Guid.NewGuid():N}"),
            email,
            request.DisplayName,
            null,
            role,
            now,
            username);
        user.SetPasswordHash(passwords.Hash(user, request.Password));
        users.Add(user);
        await users.SaveChangesAsync(ct);
        return new AuthenticatedUser(user.Id.Value, user.Email, user.DisplayName, null, user.Role.Value);
    }
}

public sealed record SignInWithPasswordCommand(string Login, string Password) : ICommand<AuthenticatedUser>;
public sealed class SignInWithPasswordValidator : AbstractValidator<SignInWithPasswordCommand>
{
    public SignInWithPasswordValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
public sealed class SignInWithPasswordHandler(
    IUserRepository users,
    IPasswordService passwords,
    TimeProvider timeProvider) : IRequestHandler<SignInWithPasswordCommand, ErrorOr<AuthenticatedUser>>
{
    public async Task<ErrorOr<AuthenticatedUser>> Handle(SignInWithPasswordCommand request, CancellationToken ct)
    {
        var user = await users.FindByLoginAsync(request.Login.Trim(), ct);
        if (user?.PasswordHash is null ||
            !passwords.Verify(user, user.PasswordHash, request.Password, out var needsRehash))
            return Error.Unauthorized("auth.local.invalid_credentials", "Login or password is incorrect.");

        if (needsRehash) user.SetPasswordHash(passwords.Hash(user, request.Password));
        user.RecordSignIn(user.Email, user.DisplayName, user.AvatarUrl, timeProvider.GetUtcNow());
        await users.SaveChangesAsync(ct);
        return new AuthenticatedUser(user.Id.Value, user.Email, user.DisplayName, user.AvatarUrl, user.Role.Value);
    }
}

public sealed record RequestPasswordResetCommand(string Email) : ICommand<bool>;
public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
}
public sealed class RequestPasswordResetHandler(
    IUserRepository users,
    IIdentityEmailSender emailSender,
    TimeProvider timeProvider) : IRequestHandler<RequestPasswordResetCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user?.PasswordHash is null) return true;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        user.IssuePasswordReset(HashToken(token), timeProvider.GetUtcNow().AddMinutes(30));
        await users.SaveChangesAsync(ct);
        await emailSender.SendPasswordResetAsync(user.Email, user.DisplayName, token, ct);
        return true;
    }

    internal static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed record ResetPasswordCommand(string Login, string Token, string Password) : ICommand<bool>;
public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Login).NotEmpty().MaximumLength(320);
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().Length(8, 128);
    }
}
public sealed class ResetPasswordHandler(
    IUserRepository users,
    IPasswordService passwords,
    TimeProvider timeProvider) : IRequestHandler<ResetPasswordCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await users.FindByLoginAsync(request.Login.Trim(), ct);
        if (user is null) return Error.Validation("auth.reset.invalid", "Reset link is invalid or expired.");
        var passwordHash = passwords.Hash(user, request.Password);
        if (!user.ResetPassword(RequestPasswordResetHandler.HashToken(request.Token), passwordHash, timeProvider.GetUtcNow()))
            return Error.Validation("auth.reset.invalid", "Reset link is invalid or expired.");
        await users.SaveChangesAsync(ct);
        return true;
    }
}
