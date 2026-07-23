using System.Text.RegularExpressions;
using ZMovie.Application.Engagement;

namespace ZMovie.Infrastructure.Recommendations.Models;

/// <summary>
/// A tiny local content model for demos. It learns no external data: title metadata is
/// tokenized into TF-IDF vectors, then a weighted user profile is cosine-ranked.
/// </summary>
public sealed class TinyTfidfRecommendationModel
{
    private static readonly Regex Word = new("[\\p{L}\\p{Nd}]{2,}", RegexOptions.Compiled);
    private readonly IReadOnlyDictionary<Guid, float[]> _vectors;

    private TinyTfidfRecommendationModel(IReadOnlyDictionary<Guid, float[]> vectors) => _vectors = vectors;

    public static TinyTfidfRecommendationModel Train(IReadOnlyList<RecommendationCandidate> candidates)
    {
        var documents = candidates.ToDictionary(x => x.TitleId, x => Tokens(x).ToArray());
        var documentFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var tokens in documents.Values)
            foreach (var token in tokens.Distinct(StringComparer.OrdinalIgnoreCase)) documentFrequency[token] = documentFrequency.GetValueOrDefault(token) + 1;

        var vocabulary = documentFrequency.Keys.Order(StringComparer.Ordinal).Select((token, index) => new { token, index }).ToDictionary(x => x.token, x => x.index, StringComparer.OrdinalIgnoreCase);
        var vectors = new Dictionary<Guid, float[]>();
        foreach (var (id, tokens) in documents)
        {
            var vector = new float[vocabulary.Count];
            foreach (var group in tokens.GroupBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var tf = (float)group.Count() / Math.Max(tokens.Length, 1);
                var idf = MathF.Log((1f + candidates.Count) / (1f + documentFrequency[group.Key])) + 1f;
                vector[vocabulary[group.Key]] = tf * idf;
            }
            Normalize(vector);
            vectors[id] = vector;
        }
        return new TinyTfidfRecommendationModel(vectors);
    }

    public IReadOnlyList<Guid> Recommend(IReadOnlyList<RecommendationSeed> profile, IReadOnlySet<Guid> excludedTitleIds, int limit)
    {
        var userVector = new float[_vectors.Values.FirstOrDefault()?.Length ?? 0];
        foreach (var seed in profile)
            if (_vectors.TryGetValue(seed.TitleId, out var vector)) AddWeighted(userVector, vector, seed.Weight);
        Normalize(userVector);

        return _vectors.Where(x => !excludedTitleIds.Contains(x.Key))
            .Select(x => new { x.Key, Score = Dot(userVector, x.Value) })
            .OrderByDescending(x => x.Score).ThenBy(x => x.Key).Take(limit).Select(x => x.Key).ToList();
    }

    private static IEnumerable<string> Tokens(RecommendationCandidate candidate)
    {
        var source = $"{candidate.Title.Title} {candidate.Title.Title} {candidate.Title.Genre} {candidate.Title.Genre} {candidate.Synopsis}";
        return Word.Matches(source.ToLowerInvariant()).Select(x => x.Value);
    }

    private static void AddWeighted(float[] target, float[] source, int weight)
    {
        for (var i = 0; i < target.Length; i++) target[i] += source[i] * weight;
    }

    private static float Dot(float[] left, float[] right)
    {
        var total = 0f;
        for (var i = 0; i < left.Length; i++) total += left[i] * right[i];
        return total;
    }

    private static void Normalize(float[] vector)
    {
        var magnitude = MathF.Sqrt(vector.Sum(x => x * x));
        if (magnitude <= 0) return;
        for (var i = 0; i < vector.Length; i++) vector[i] /= magnitude;
    }
}
