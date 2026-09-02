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
            user = User.Create(UserId.New(), externalIdentity, identity.Email, identity.DisplayName, identity.AvatarUrl, initialRole, now);
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
