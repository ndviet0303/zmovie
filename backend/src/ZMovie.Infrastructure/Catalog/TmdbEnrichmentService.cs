using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ZMovie.Infrastructure.Catalog;

public sealed record TmdbTitleData(
    string? PosterPath,
    string? BackdropPath,
    string? TrailerYoutubeKey,
    IReadOnlyList<string> Cast,
    string? Director,
    double? VoteAverage)
{
    public string? GetBackdrop4kUrl() =>
        string.IsNullOrWhiteSpace(BackdropPath) ? null : $"https://image.tmdb.org/t/p/original{BackdropPath}";

    public string? GetTrailerUrl() =>
        string.IsNullOrWhiteSpace(TrailerYoutubeKey) ? null : $"https://www.youtube.com/watch?v={TrailerYoutubeKey}";
}

public interface ITmdbClient
{
    Task<TmdbTitleData?> SearchAndGetDetailsAsync(string query, int? year, CancellationToken ct);
}

public sealed class TmdbClient(HttpClient httpClient, IConfiguration config, ILogger<TmdbClient> logger) : ITmdbClient
{
    private readonly string? _apiKey = config["TMDB:ApiKey"] ?? config["TMDB_API_KEY"];

    public async Task<TmdbTitleData?> SearchAndGetDetailsAsync(string query, int? year, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        try
        {
            var searchUrl = $"https://api.themoviedb.org/3/search/multi?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&language=vi-VN";
            if (year.HasValue) searchUrl += $"&year={year.Value}";

            var searchRes = await httpClient.GetFromJsonAsync<TmdbSearchResponse>(searchUrl, ct);
            var firstResult = searchRes?.Results?.FirstOrDefault();
            if (firstResult == null) return null;

            var mediaType = string.Equals(firstResult.MediaType, "tv", StringComparison.OrdinalIgnoreCase) ? "tv" : "movie";
            var detailsUrl = $"https://api.themoviedb.org/3/{mediaType}/{firstResult.Id}?api_key={_apiKey}&append_to_response=videos,credits&language=vi-VN";

            var details = await httpClient.GetFromJsonAsync<TmdbDetailsResponse>(detailsUrl, ct);
            if (details == null) return null;

            var trailer = details.Videos?.Results?.FirstOrDefault(v =>
                string.Equals(v.Site, "YouTube", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(v.Type, "Trailer", StringComparison.OrdinalIgnoreCase) || string.Equals(v.Type, "Teaser", StringComparison.OrdinalIgnoreCase)));

            var cast = details.Credits?.Cast?.Take(8).Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? [];
            var director = details.Credits?.Crew?.FirstOrDefault(c => string.Equals(c.Job, "Director", StringComparison.OrdinalIgnoreCase))?.Name;

            return new TmdbTitleData(
                details.PosterPath ?? firstResult.PosterPath,
                details.BackdropPath ?? firstResult.BackdropPath,
                trailer?.Key,
                cast,
                director,
                details.VoteAverage ?? firstResult.VoteAverage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enrich TMDB metadata for query {Query}", query);
            return null;
        }
    }

    private sealed class TmdbSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbSearchResult>? Results { get; set; }
    }

    private sealed class TmdbSearchResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }
    }

    private sealed class TmdbDetailsResponse
    {
        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }

        [JsonPropertyName("videos")]
        public TmdbVideosPayload? Videos { get; set; }

        [JsonPropertyName("credits")]
        public TmdbCreditsPayload? Credits { get; set; }
    }

    private sealed class TmdbVideosPayload
    {
        [JsonPropertyName("results")]
        public List<TmdbVideoItem>? Results { get; set; }
    }

    private sealed class TmdbVideoItem
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("site")]
        public string? Site { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    private sealed class TmdbCreditsPayload
    {
        [JsonPropertyName("cast")]
        public List<TmdbCastItem>? Cast { get; set; }

        [JsonPropertyName("crew")]
        public List<TmdbCrewItem>? Crew { get; set; }
    }

    private sealed class TmdbCastItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TmdbCrewItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("job")]
        public string? Job { get; set; }
    }
}
