using System.Text.RegularExpressions;

namespace ZMovie.Application.Assistant;

public static class AssistantMood
{
    private static readonly Regex Word = new("[\\p{L}\\p{Nd}]{2,}", RegexOptions.Compiled);
    private static readonly MoodRule[] Rules =
    [
        new(
            ["buồn", "cô đơn", "cô độc", "cần an ủi", "sad", "lonely", "down", "heartbroken"],
            ["chữa lành", "healing", "comfort", "ấm áp", "warm", "gentle", "nhẹ nhàng", "hy vọng", "hope", "uplifting", "tình bạn", "friendship", "gia đình", "family", "lãng mạn", "romance"]),
        new(
            ["stress", "căng thẳng", "mệt", "mệt mỏi", "thư giãn", "relax", "tired"],
            ["nhẹ nhàng", "gentle", "hài", "comedy", "vui", "fun", "giải trí", "feel good", "healing", "chữa lành"]),
    ];

    public static bool WantsComfort(string? message) =>
        !string.IsNullOrWhiteSpace(message) && Rules.Any(rule => rule.Triggers.Any(message.Contains));

    public static IReadOnlyDictionary<string, int> SearchTermWeights(string? message)
    {
        var weights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(message)) return weights;

        foreach (var token in Words(message)) weights[token] = 1;
        foreach (var rule in Rules)
        {
            if (!rule.Triggers.Any(message.Contains)) continue;
            foreach (var token in rule.Terms.SelectMany(Words))
                weights[token] = Math.Max(weights.GetValueOrDefault(token), 3);
        }

        return weights;
    }

    private static IEnumerable<string> Words(string value) =>
        Word.Matches(value.ToLowerInvariant()).Select(match => match.Value).Distinct(StringComparer.OrdinalIgnoreCase);

    private sealed record MoodRule(IReadOnlyList<string> Triggers, IReadOnlyList<string> Terms);
}
