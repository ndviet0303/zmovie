using ErrorOr;
using MediatR;
using ZMovie.Application.Common;

namespace ZMovie.Application.Identity;

public sealed record GoogleIdentity(string Subject, string Email, string DisplayName, string? AvatarUrl);
public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName, string? AvatarUrl);
public interface IGoogleIdentityVerifier { Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct); }
public interface IUserIdentityStore { Task<AuthenticatedUser> UpsertGoogleUserAsync(GoogleIdentity identity, CancellationToken ct); }

public sealed record SignInWithGoogleCommand(string Credential) : ICommand<AuthenticatedUser>;
public sealed class SignInWithGoogleHandler(IGoogleIdentityVerifier verifier, IUserIdentityStore users) : IRequestHandler<SignInWithGoogleCommand, ErrorOr<AuthenticatedUser>>
{
    public async Task<ErrorOr<AuthenticatedUser>> Handle(SignInWithGoogleCommand request, CancellationToken ct)
        => await verifier.VerifyAsync(request.Credential, ct) is { } identity
            ? await users.UpsertGoogleUserAsync(identity, ct)
            : Error.Unauthorized("auth.google.invalid_credential", "Google credential is invalid or expired.");
}
