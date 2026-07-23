using Microsoft.Extensions.Caching.Memory;
using ZMovie.Application.Engagement;
using ZMovie.Infrastructure.Recommendations.Models;

namespace ZMovie.Infrastructure.Recommendations;

public sealed class TinyContentRecommendationEngine(IMemoryCache cache) : IRecommendationEngine
{
    public IReadOnlyList<Guid> Recommend(IReadOnlyList<RecommendationCandidate> candidates, IReadOnlyList<RecommendationSeed> profile, IReadOnlySet<Guid> excludedTitleIds, int limit)
    {
        var signature = string.Join('|', candidates.Select(x => $"{x.TitleId:N}:{x.Title.Title}:{x.Synopsis.Length}").Order(StringComparer.Ordinal));
        var model = cache.GetOrCreate($"recommendations:tfidf:{signature}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            entry.Size = 10;
            return TinyTfidfRecommendationModel.Train(candidates);
        })!;
        return model.Recommend(profile, excludedTitleIds, limit);
    }
}
