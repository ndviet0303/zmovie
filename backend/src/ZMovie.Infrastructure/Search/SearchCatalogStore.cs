using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZMovie.Application.Catalog;
using ZMovie.Application.Search;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Infrastructure.Search;

public sealed class SearchCatalogStore(HttpClient http, IConfiguration config, CatalogDbContext db) : ISearchCatalogStore
{
    private readonly HttpClient _http = Configure(http, config);
    public async Task<TitleListResponse> SearchAsync(string query, string? type, string? genre, string locale, CancellationToken ct)
    {
        try
        {
            var filters = new[] { string.IsNullOrWhiteSpace(type) ? null : $"type = '{type.Replace("'", "\\'")}'", string.IsNullOrWhiteSpace(genre) ? null : $"genre = '{genre.Replace("'", "\\'")}'" }.Where(x => x is not null).ToArray();
            var response = await _http.PostAsJsonAsync("/indexes/zmovie_titles/search", new { q = query, filter = filters }, ct);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            var hits = json.RootElement.GetProperty("hits").Deserialize<List<Document>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            var items = hits.Select(x => new TitleSummary(x.Slug, locale == "en" ? x.EnglishTitle : x.VietnameseTitle, x.Genre, x.Year, x.Type, x.PosterUrl)).ToList();
            return new(items, items.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var titles = await db.Titles.AsNoTracking().Where(x => (string.IsNullOrEmpty(query) || x.EnglishTitle.Contains(query) || x.VietnameseTitle.Contains(query)) && (string.IsNullOrEmpty(type) || x.Type == type) && (string.IsNullOrEmpty(genre) || EF.Functions.ILike(x.Genre, $"%{genre}%"))).ToListAsync(ct);
            var items = titles.Select(x => new TitleSummary(x.Slug, x.LocalizedTitle(locale), x.Genre, x.Year, x.Type, x.PosterUrl)).ToList();
            return new(items, items.Count);
        }
    }
    private static HttpClient Configure(HttpClient http, IConfiguration config)
    {
        http.BaseAddress = new Uri(config["Meilisearch:Url"] ?? "http://localhost:7700");
        http.Timeout = TimeSpan.FromSeconds(10);
        var key = config["Meilisearch:ApiKey"];
        if (!string.IsNullOrWhiteSpace(key)) http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
        return http;
    }
    private sealed record Document(string Slug, string EnglishTitle, string VietnameseTitle, string Genre, int Year, string Type, string PosterUrl);
}
