using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Assistant;
using ZMovie.Application.Catalog;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Assistant;

public sealed class CatalogAssistantStore(CatalogDbContext db) : ICatalogAssistantStore
{
    private static readonly Regex Word = new("[\\p{L}\\p{Nd}]{2,}", RegexOptions.Compiled);

    public async Task<IReadOnlyList<AssistantCatalogTitle>> SearchAsync(string message, string locale, int limit, CancellationToken ct)
    {
        var tokens = Word.Matches(message.ToLowerInvariant()).Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (tokens.Length == 0) return [];
        var titles = await db.Titles.AsNoTracking().ToListAsync(ct);
        return titles.Select(title => new
            {
                Item = new AssistantCatalogTitle(new TitleSummary(title.Slug, title.LocalizedTitle(locale), title.Genre, title.Year, title.Type, title.PosterUrl), title.LocalizedSynopsis(locale)),
                Score = Score(title, tokens, locale),
            })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Item.Title.Year).Take(limit).Select(x => x.Item).ToList();
    }

    private static int Score(ZMovie.Domain.Catalog.CatalogTitle title, string[] tokens, string locale)
    {
        var name = title.LocalizedTitle(locale).ToLowerInvariant();
        var text = $"{name} {title.Genre} {title.LocalizedSynopsis(locale)}".ToLowerInvariant();
        return tokens.Sum(token => (name.Contains(token) ? 5 : 0) + (title.Genre.Contains(token, StringComparison.OrdinalIgnoreCase) ? 3 : 0) + (text.Contains(token) ? 1 : 0));
    }
}
