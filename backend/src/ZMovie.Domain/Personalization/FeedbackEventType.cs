namespace ZMovie.Domain.Personalization;

public readonly record struct FeedbackEventType : IComparable<FeedbackEventType>, IComparable
{
    public const string ImpressionName = "impression";
    public const string ClickName = "click";
    public const string SaveName = "save";
    public const string WatchName = "watch";
    public const string CompleteName = "complete";
    public const string LikeName = "like";
    public const string DislikeName = "dislike";

    public static readonly FeedbackEventType Impression = new(ImpressionName);
    public static readonly FeedbackEventType Click = new(ClickName);
    public static readonly FeedbackEventType Save = new(SaveName);
    public static readonly FeedbackEventType Watch = new(WatchName);
    public static readonly FeedbackEventType Complete = new(CompleteName);
    public static readonly FeedbackEventType Like = new(LikeName);
    public static readonly FeedbackEventType Dislike = new(DislikeName);

    public string Value { get; }

    private FeedbackEventType(string value) => Value = value;

    public static bool TryParse(string? raw, out FeedbackEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            eventType = default;
            return false;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case ImpressionName:
                eventType = Impression;
                return true;
            case ClickName:
                eventType = Click;
                return true;
            case SaveName:
                eventType = Save;
                return true;
            case WatchName:
                eventType = Watch;
                return true;
            case CompleteName:
                eventType = Complete;
                return true;
            case LikeName:
                eventType = Like;
                return true;
            case DislikeName:
                eventType = Dislike;
                return true;
            default:
                eventType = default;
                return false;
        }
    }

    public static FeedbackEventType Parse(string raw)
    {
        if (!TryParse(raw, out var eventType))
        {
            throw new ArgumentException($"Invalid feedback event type '{raw}'.", nameof(raw));
        }

        return eventType;
    }

    public bool IsImpression => Value == ImpressionName;

    public override string ToString() => Value;
    public int CompareTo(FeedbackEventType other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public int CompareTo(object? obj) => obj is FeedbackEventType other ? CompareTo(other) : 1;
}

public static class RewardPolicy
{
    private static readonly IReadOnlyDictionary<string, double> Rewards = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        [FeedbackEventType.ImpressionName] = 0.0,
        [FeedbackEventType.ClickName] = 0.5,
        [FeedbackEventType.SaveName] = 2.0,
        [FeedbackEventType.WatchName] = 3.0,
        [FeedbackEventType.CompleteName] = 5.0,
        [FeedbackEventType.LikeName] = 4.0,
        [FeedbackEventType.DislikeName] = -4.0,
    };

    public static bool TryGetReward(string? eventTypeName, out double reward)
    {
        if (eventTypeName is not null && Rewards.TryGetValue(eventTypeName.Trim(), out reward))
        {
            return true;
        }

        reward = 0.0;
        return false;
    }

    public static double GetReward(FeedbackEventType eventType) =>
        Rewards.TryGetValue(eventType.Value, out var reward) ? reward : 0.0;
}
