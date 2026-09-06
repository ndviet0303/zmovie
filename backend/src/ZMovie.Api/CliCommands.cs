using Microsoft.EntityFrameworkCore;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Seed;
using ZMovie.Infrastructure.Storage;

namespace ZMovie.Api;

public static class CliCommands
{
    public static async Task<bool> TryHandleAsync(string[] args, IServiceProvider services)
    {
        if (args.Contains("--import-ophim-genres", StringComparer.OrdinalIgnoreCase))
        {
            await using var importScope = services.CreateAsyncScope();
            var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var httpClient = importScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            await importDb.Database.MigrateAsync();
            var imported = await OPhimGenreImporter.ImportAsync(importDb, httpClient, CancellationToken.None);
            Console.WriteLine($"Imported {imported} OPhim genres into genres.");
            return true;
        }

        if (args.Contains("--import-ophim-catalog", StringComparer.OrdinalIgnoreCase))
        {
            var maxPages = ReadIntegerOption(args, "--max-pages");
            var startPage = ReadIntegerOption(args, "--start-page") ?? 1;
            var importAll = args.Contains("--all", StringComparer.OrdinalIgnoreCase);
            var includeEpisodes = args.Contains("--with-episodes", StringComparer.OrdinalIgnoreCase);
            var detailConcurrency = ReadIntegerOption(args, "--concurrency") ?? 3;
            if (detailConcurrency is < 1 or > 8) throw new ArgumentOutOfRangeException("--concurrency", "Use a value from 1 to 8.");
            if (!importAll && maxPages is null) maxPages = 1;

            await using var importScope = services.CreateAsyncScope();
            var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var httpClient = importScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            await importDb.Database.MigrateAsync();
            var options = new OPhimCatalogImportOptions(maxPages, startPage, includeEpisodes, TimeSpan.FromMilliseconds(300))
            {
                DetailConcurrency = detailConcurrency,
            };
            var imported = await OPhimCatalogImporter.ImportAsync(importDb, httpClient, options, Console.WriteLine, CancellationToken.None);
            Console.WriteLine($"Imported {imported.TitlesImported} OPhim titles from {imported.PagesImported} pages (source total: {imported.TotalItems}; episodes: {imported.EpisodesImported}).");
            return true;
        }

        if (args.Contains("--import-nguonc-catalog", StringComparer.OrdinalIgnoreCase))
        {
            var maxPages = ReadIntegerOption(args, "--max-pages");
            var startPage = ReadIntegerOption(args, "--start-page") ?? 1;
            var importAll = args.Contains("--all", StringComparer.OrdinalIgnoreCase);
            var includeEpisodes = !args.Contains("--without-episodes", StringComparer.OrdinalIgnoreCase);
            var detailConcurrency = ReadIntegerOption(args, "--concurrency") ?? 12;
            if (detailConcurrency is < 1 or > 32) throw new ArgumentOutOfRangeException("--concurrency", "Use a value from 1 to 32.");
            var delayMs = ReadIntegerOption(args, "--delay-ms") ?? 50;
            if (!importAll && maxPages is null) maxPages = 1;

            await using var importScope = services.CreateAsyncScope();
            var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var httpClient = importScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            await importDb.Database.MigrateAsync();
            var options = new NguonCCatalogImportOptions(maxPages, startPage, includeEpisodes, TimeSpan.FromMilliseconds(delayMs))
            {
                DetailConcurrency = detailConcurrency,
            };
            var imported = await NguonCCatalogImporter.ImportAsync(importDb, httpClient, options, Console.WriteLine, CancellationToken.None);
            Console.WriteLine($"Imported {imported.TitlesImported} NguonC titles from {imported.PagesImported} pages (source total: {imported.TotalItems}; episodes: {imported.EpisodesImported}).");
            return true;
        }

        if (args.Contains("--seed-r2-demo", StringComparer.OrdinalIgnoreCase))
        {
            await using var seedScope = services.CreateAsyncScope();
            var seedDb = seedScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var r2Storage = seedScope.ServiceProvider.GetRequiredService<ICloudflareR2Storage>();
            await seedDb.Database.MigrateAsync();
            var count = await R2DemoCatalogSeed.SeedAsync(seedDb, r2Storage.GetPublicStreamUrl);
            Console.WriteLine($"Seeded {count} Cloudflare R2 benchmark demo titles successfully.");
            return true;
        }

        return false;
    }

    private static int? ReadIntegerOption(string[] args, string optionName)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index + 1 >= args.Length) return null;
        return int.TryParse(args[index + 1], out var parsed) ? parsed : null;
    }
}
