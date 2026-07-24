using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZMovie.Application.Assistant;

namespace ZMovie.Infrastructure.Assistant;

public sealed class LocalAiOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://127.0.0.1:8788";
    public int TimeoutSeconds { get; set; } = 15;
}

public sealed class LocalAiAssistantTextGenerator(HttpClient http, IOptions<LocalAiOptions> options, ILogger<LocalAiAssistantTextGenerator> logger) : IAssistantTextGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string?> GenerateAsync(AssistantGenerationRequest request, CancellationToken ct)
    {
        if (!options.Value.Enabled) return null;

        try
        {
            using var response = await http.PostAsJsonAsync("v1/chat", new
            {
                message = request.Message,
                locale = request.Locale,
                matches = request.Matches,
            }, JsonOptions, ct);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<LocalAiResponse>(JsonOptions, ct);
            return string.IsNullOrWhiteSpace(result?.Reply) ? null : result.Reply.Trim();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Local AI service timed out while generating an assistant reply.");
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Local AI service is unavailable; using the deterministic assistant reply.");
            return null;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Local AI service configuration is invalid; using the deterministic assistant reply.");
            return null;
        }
    }

    private sealed record LocalAiResponse(string? Reply);
}
