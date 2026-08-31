namespace ZMovie.Domain.Identity;

public readonly record struct Role
{
    public const string MemberName = "member";
    public const string AdminName = "admin";

    public static readonly Role Member = new(MemberName);
    public static readonly Role Admin = new(AdminName);

    private Role(string value) => Value = value;

    public string Value { get; } = MemberName;

    public bool IsAdmin => Value == AdminName;

    public static bool TryCreate(string? value, out Role role)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is MemberName or AdminName)
        {
            role = new Role(normalized);
            return true;
        }

        role = default;
        return false;
    }

    public static Role Normalize(string? value) =>
        TryCreate(value, out var role) ? role : Member;

    public static bool IsKnown(string? value) =>
        value?.Trim().ToLowerInvariant() is MemberName or AdminName;

    public override string ToString() => Value;
}
