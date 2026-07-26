namespace ZMovie.Domain.Identity;

public static class ZMovieRoles
{
    public const string Member = "member";
    public const string Admin = "admin";

    /// <summary>Authorization policy name used by the API for the whole /v1/admin surface.</summary>
    public const string AdminPolicy = "ZMovie.Admin";

    /// <summary>
    /// Whether the caller supplied a role we recognise. Deliberately not expressed via
    /// <see cref="Normalize"/>, which maps anything unknown onto <see cref="Member"/> —
    /// routing an unrecognised role through that would silently demote a user instead of
    /// rejecting the request.
    /// </summary>
    public static bool IsKnown(string? role) => role?.Trim().ToLowerInvariant() is Member or Admin;

    public static bool IsAdmin(string? role) => string.Equals(Normalize(role), Admin, StringComparison.Ordinal);

    public static string Normalize(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        Admin => Admin,
        _ => Member,
    };
}
