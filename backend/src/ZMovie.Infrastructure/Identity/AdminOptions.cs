namespace ZMovie.Infrastructure.Identity;

/// <summary>
/// Bootstrap allowlist for the admin role. Configured as <c>Admin:Emails:0</c>, <c>Admin:Emails:1</c>, …
/// (or <c>Admin__Emails__0</c> as an environment variable / Infisical key).
/// A user whose verified Google email matches an entry is promoted to admin on every sign-in.
/// Removing an entry does not demote anyone — revoke through the admin UI instead.
/// </summary>
public sealed class AdminOptions
{
    public IReadOnlyList<string> Emails { get; set; } = [];

    public bool IsAllowlisted(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && Emails.Any(entry => !string.IsNullOrWhiteSpace(entry) && string.Equals(entry.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
