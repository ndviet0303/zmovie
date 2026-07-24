using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Persistence;

namespace ZMovie.Api.Services;

public sealed record OPhimCrawlerStartOptions(int StartPage, int? EndPage, bool IncludeEpisodes);

public sealed record OPhimCrawlerStatus(
    bool IsRunning,
    bool CancelRequested,
    int StartPage,
    int? EndPage,
    bool IncludeEpisodes,
    int CurrentPage,
    int TotalPages,
    int TitlesImported,
    int EpisodesImported,
    string Message,
    string? Error,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt);

public sealed class OPhimCrawlerService(IServiceScopeFactory scopeFactory) : IDisposable
{
    private static readonly Regex ProgressPattern = new(
        @"page\s+(?<page>\d+)\/(?<total>\d+)\s+\((?<titles>\d+)\s+titles,\s+(?<episodes>\d+)\s+episodes\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly object gate = new();
    private CancellationTokenSource? cancellation;
    private Task? runningTask;
    private OPhimCrawlerStatus status = IdleStatus();

    public OPhimCrawlerStatus GetStatus()
    {
        lock (gate) return status;
    }

    public bool TryStart(OPhimCrawlerStartOptions options)
    {
        if (options.StartPage < 1 || (options.EndPage is not null && options.EndPage < options.StartPage)) return false;

        lock (gate)
        {
            if (status.IsRunning) return false;

            cancellation = new CancellationTokenSource();
            status = new OPhimCrawlerStatus(
                true,
                false,
                options.StartPage,
                options.EndPage,
                options.IncludeEpisodes,
                options.StartPage,
                0,
                0,
                0,
                "Đang khởi tạo crawler…",
                null,
                DateTimeOffset.UtcNow,
                null);
            runningTask = RunAsync(options, cancellation.Token);
            return true;
        }
    }

    public bool TryStop()
    {
        lock (gate)
        {
            if (!status.IsRunning || cancellation is null) return false;
            status = status with { CancelRequested = true, Message = "Đang dừng sau batch hiện tại…" };
            cancellation.Cancel();
            return true;
        }
    }

    private async Task RunAsync(OPhimCrawlerStartOptions options, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Database.MigrateAsync(ct);

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            int? maxPages = options.EndPage is null ? null : options.EndPage.Value - options.StartPage + 1;
            var importerOptions = new OPhimCatalogImportOptions(
                maxPages,
                options.StartPage,
                options.IncludeEpisodes,
                TimeSpan.FromMilliseconds(300));

            var result = await OPhimCatalogImporter.ImportAsync(
                db,
                http,
                importerOptions,
                Report,
                ct);

            lock (gate)
            {
                status = status with
                {
                    IsRunning = false,
                    CurrentPage = status.TotalPages > 0 ? status.TotalPages : status.CurrentPage,
                    TitlesImported = result.TitlesImported,
                    EpisodesImported = result.EpisodesImported,
                    Message = $"Hoàn tất: {result.TitlesImported:N0} phim, {result.EpisodesImported:N0} episodes.",
                    FinishedAt = DateTimeOffset.UtcNow,
                };
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            lock (gate) status = status with { IsRunning = false, Message = "Đã dừng crawler.", FinishedAt = DateTimeOffset.UtcNow };
        }
        catch (Exception exception)
        {
            lock (gate)
            {
                status = status with
                {
                    IsRunning = false,
                    Message = "Crawler gặp lỗi.",
                    Error = exception.GetBaseException().Message,
                    FinishedAt = DateTimeOffset.UtcNow,
                };
            }
        }
        finally
        {
            lock (gate)
            {
                cancellation?.Dispose();
                cancellation = null;
                runningTask = null;
            }
        }
    }

    private void Report(string message)
    {
        var match = ProgressPattern.Match(message);
        lock (gate)
        {
            if (!match.Success)
            {
                status = status with { Message = message };
                return;
            }

            status = status with
            {
                CurrentPage = int.Parse(match.Groups["page"].Value),
                TotalPages = int.Parse(match.Groups["total"].Value),
                TitlesImported = int.Parse(match.Groups["titles"].Value),
                EpisodesImported = int.Parse(match.Groups["episodes"].Value),
                Message = $"Đã crawl page {match.Groups["page"].Value}/{match.Groups["total"].Value}",
                Error = null,
            };
        }
    }

    private static OPhimCrawlerStatus IdleStatus() => new(
        false,
        false,
        1,
        null,
        false,
        0,
        0,
        0,
        0,
        "Sẵn sàng.",
        null,
        null,
        null);

    public void Dispose()
    {
        lock (gate) cancellation?.Cancel();
    }
}
