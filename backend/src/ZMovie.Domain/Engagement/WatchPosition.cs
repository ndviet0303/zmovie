namespace ZMovie.Domain.Engagement;

public readonly record struct WatchPosition
{
    private WatchPosition(double seconds)
    {
        Seconds = seconds;
    }

    public double Seconds { get; }

    public static WatchPosition FromSeconds(double seconds) =>
        new(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 ? 0 : seconds);
}
