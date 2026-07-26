using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Common;

namespace ZMovie.Application.Identity;

public sealed record GoogleIdentity(string Subject, string Email, string DisplayName, string? AvatarUrl);
public sealed record AuthenticatedUser(Guid Id, string Email, string DisplayName, string? AvatarUrl, string Role);
public interface IGoogleIdentityVerifier { Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct); }
public interface IUserIdentityStore { Task<AuthenticatedUser> UpsertGoogleUserAsync(GoogleIdentity identity, CancellationToken ct); }

public sealed record SignInWithGoogleCommand(string Credential) : ICommand<AuthenticatedUser>;
public sealed class SignInWithGoogleValidator : AbstractValidator<SignInWithGoogleCommand>
{
    // A Google ID token is well under 4 KB; the endpoint is anonymous so the bound
    // credential must be length-capped before it reaches the JWT validator.
    public SignInWithGoogleValidator() => RuleFor(x => x.Credential).NotEmpty().MaximumLength(8_192);
}
public sealed class SignInWithGoogleHandler(IGoogleIdentityVerifier verifier, IUserIdentityStore users) : IRequestHandler<SignInWithGoogleCommand, ErrorOr<AuthenticatedUser>>
{
    public async Task<ErrorOr<AuthenticatedUser>> Handle(SignInWithGoogleCommand request, CancellationToken ct)
        => await verifier.VerifyAsync(request.Credential, ct) is { } identity
            ? await users.UpsertGoogleUserAsync(identity, ct)
            : Error.Unauthorized("auth.google.invalid_credential", "Google credential is invalid or expired.");
}
