namespace ZMovie.Domain.Catalog;

public readonly record struct LocalizedText
{
    public LocalizedText(string vietnamese, string english)
    {
        Vietnamese = vietnamese?.Trim() ?? string.Empty;
        English = english?.Trim() ?? string.Empty;
    }

    public string Vietnamese { get; }
    public string English { get; }

    public string Localize(string? locale) =>
        locale is not null && locale.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? English
            : Vietnamese;

    public override string ToString() => $"[vi: {Vietnamese}, en: {English}]";
}
