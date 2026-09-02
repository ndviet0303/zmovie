namespace ZMovie.Domain.Catalog;

public readonly record struct Runtime
{
    private Runtime(int minutes) => Minutes = minutes;

    public int Minutes { get; }

    public static bool TryCreate(int minutes, out Runtime runtime)
    {
        if (minutes is >= 0 and <= 100_000)
        {
            runtime = new Runtime(minutes);
            return true;
        }

        runtime = default;
        return false;
    }

    public static Runtime FromMinutes(int minutes) =>
        TryCreate(minutes, out var runtime) ? runtime : new Runtime(0);

    public override string ToString() => $"{Minutes} min";
}
