using System.Text.RegularExpressions;

namespace ZMovie.Domain.Catalog;

public readonly partial record struct TitleSlug
{
    private readonly string? _value;

    private TitleSlug(string value) => _value = value;

    public string Value => _value ?? string.Empty;

    public static bool TryCreate(string? value, out TitleSlug slug)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            slug = default;
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length <= 160 && SlugRegex().IsMatch(normalized))
        {
            slug = new TitleSlug(normalized);
            return true;
        }

        slug = default;
        return false;
    }

    public static TitleSlug Parse(string value) =>
        TryCreate(value, out var slug)
            ? slug
            : throw new ArgumentException($"Invalid title slug: '{value}'.", nameof(value));

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();
}
