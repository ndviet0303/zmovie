namespace ZMovie.Domain.Catalog;

public readonly record struct TitleType
{
    public const string MovieName = "movie";
    public const string SeriesName = "series";

    public static readonly TitleType Movie = new(MovieName);
    public static readonly TitleType Series = new(SeriesName);

    private TitleType(string value) => Value = value;

    public string Value { get; } = MovieName;

    public bool IsMovie => Value == MovieName;
    public bool IsSeries => Value == SeriesName;

    public static bool TryCreate(string? value, out TitleType titleType)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is MovieName or SeriesName)
        {
            titleType = new TitleType(normalized);
            return true;
        }

        titleType = default;
        return false;
    }

    public static TitleType Normalize(string? value) =>
        TryCreate(value, out var type) ? type : Movie;

    public static bool IsKnown(string? value) =>
        value?.Trim().ToLowerInvariant() is MovieName or SeriesName;

    public override string ToString() => Value;
}
