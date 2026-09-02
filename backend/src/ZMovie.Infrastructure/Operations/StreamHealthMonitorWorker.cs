using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZMovie.Infrastructure.Operations;

public interface IStreamHealthChecker
{
    Task<bool> CheckStreamAsync(string url, CancellationToken ct = default);
}

public sealed class StreamHealthChecker(HttpClient httpClient, ILogger<StreamHealthChecker> logger) : IStreamHealthChecker
{
    public async Task<bool> CheckStreamAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            req.Headers.Add("User-Agent", "ZMovie-HealthMonitor/1.0");

            using var res = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (res.IsSuccessStatusCode)
            {
                return true;
            }

            // Some CDNs reject HEAD requests; fallback to GET with tiny byte range
            if (res.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                using var getReq = new HttpRequestMessage(HttpMethod.Get, url);
                getReq.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 1024);
                using var getRes = await httpClient.SendAsync(getReq, HttpCompletionOption.ResponseHeadersRead, ct);
                return getRes.IsSuccessStatusCode;
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Health check failed for stream {Url}", url);
            return false;
        }
    }
}

public sealed class StreamHealthMonitorWorker(
    IStreamHealthChecker checker,
    ITelegramAlertService telegram,
    ILogger<StreamHealthMonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StreamHealthMonitorWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Routine liveness check every 60 minutes
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in StreamHealthMonitorWorker loop");
            }
        }
    }

    public async Task<bool> VerifyAndAlertAsync(string titleSlug, int episodeNumber, string streamUrl, CancellationToken ct)
    {
        var isHealthy = await checker.CheckStreamAsync(streamUrl, ct);
        if (!isHealthy)
        {
            await telegram.SendStreamOutageAlertAsync(titleSlug, episodeNumber, streamUrl, 502, ct);
        }
        return isHealthy;
    }
}

