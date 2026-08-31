namespace ZMovie.Domain.Catalog;

public readonly record struct ReleaseYear
{
    public const int MinYear = 1888;
    public const int MaxYear = 2100;
    public const int UnknownYear = 0;

    public static readonly ReleaseYear Unknown = new(UnknownYear);

    private ReleaseYear(int value) => Value = value;

    public int Value { get; }

    public bool IsKnown => Value is >= MinYear and <= MaxYear;

    public static bool TryCreate(int value, out ReleaseYear releaseYear)
    {
        if (value is >= MinYear and <= MaxYear or UnknownYear)
        {
            releaseYear = new ReleaseYear(value);
            return true;
        }

        releaseYear = default;
        return false;
    }

    public static ReleaseYear FromInt(int value) =>
        TryCreate(value, out var year) ? year : Unknown;

    public override string ToString() => IsKnown ? Value.ToString() : "Unknown";
}
