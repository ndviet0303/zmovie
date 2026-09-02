using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using ZMovie.Api;
using ZMovie.Api.Configuration;
using ZMovie.Api.Endpoints;
using ZMovie.Application;
using ZMovie.Domain.Identity;
using ZMovie.Infrastructure;
using ZMovie.Infrastructure.Analytics.Persistence;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Engagement.Persistence;
using ZMovie.Infrastructure.Identity.Persistence;
using ZMovie.Infrastructure.Personalization.Persistence;
using ZMovie.Infrastructure.Seed;
using ZMovie.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);
var exposeDetailedErrors = builder.Configuration.GetValue<bool>("ExposeDetailedErrors");

await builder.Configuration.AddInfisicalSecretsAsync(builder.Environment);
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "zmovie.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiAuthorizationPolicies.AdminPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(Role.AdminName));
builder.Services.AddZMovieApplication();
builder.Services.AddZMovieInfrastructure(builder.Configuration);
builder.Services.AddSignalR();

var app = builder.Build();

if (args.Contains("--import-ophim-genres", StringComparer.OrdinalIgnoreCase))
{
    await using var importScope = app.Services.CreateAsyncScope();
    var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var httpClient = importScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
    await importDb.Database.MigrateAsync();
    var imported = await OPhimGenreImporter.ImportAsync(importDb, httpClient, CancellationToken.None);
    Console.WriteLine($"Imported {imported} OPhim genres into genres.");
    return;
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

    await using var importScope = app.Services.CreateAsyncScope();
    var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var httpClient = importScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
    await importDb.Database.MigrateAsync();
    var options = new OPhimCatalogImportOptions(maxPages, startPage, includeEpisodes, TimeSpan.FromMilliseconds(300))
    {
        DetailConcurrency = detailConcurrency,
    };
    var imported = await OPhimCatalogImporter.ImportAsync(importDb, httpClient, options, Console.WriteLine, CancellationToken.None);
    Console.WriteLine($"Imported {imported.TitlesImported} OPhim titles from {imported.PagesImported} pages (source total: {imported.TotalItems}; episodes: {imported.EpisodesImported}).");
    return;
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

    await using var importScope = app.Services.CreateAsyncScope();
    var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var httpClient = importScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
    await importDb.Database.MigrateAsync();
    var options = new NguonCCatalogImportOptions(maxPages, startPage, includeEpisodes, TimeSpan.FromMilliseconds(delayMs))
    {
        DetailConcurrency = detailConcurrency,
    };
    var imported = await NguonCCatalogImporter.ImportAsync(importDb, httpClient, options, Console.WriteLine, CancellationToken.None);
    Console.WriteLine($"Imported {imported.TitlesImported} NguonC titles from {imported.PagesImported} pages (source total: {imported.TotalItems}; episodes: {imported.EpisodesImported}).");
    return;
}

if (args.Contains("--seed-r2-demo", StringComparer.OrdinalIgnoreCase))
{
    await using var seedScope = app.Services.CreateAsyncScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var r2Storage = seedScope.ServiceProvider.GetRequiredService<ICloudflareR2Storage>();
    await seedDb.Database.MigrateAsync();
    var count = await R2DemoCatalogSeed.SeedAsync(seedDb, r2Storage.GetPublicStreamUrl);
    Console.WriteLine($"Seeded {count} Cloudflare R2 benchmark demo titles successfully.");
    return;
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    if (string.Equals(catalogDb.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var engagementDb = scope.ServiceProvider.GetRequiredService<EngagementDbContext>();
        var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var personalizationDb = scope.ServiceProvider.GetRequiredService<PersonalizationDbContext>();

        await catalogDb.Database.MigrateAsync();
        await identityDb.Database.MigrateAsync();
        await engagementDb.Database.MigrateAsync();
        await analyticsDb.Database.MigrateAsync();
        await personalizationDb.Database.MigrateAsync();

        await CatalogSeed.SeedAsync(catalogDb);
        var r2Storage = scope.ServiceProvider.GetRequiredService<ICloudflareR2Storage>();
        await R2DemoCatalogSeed.SeedAsync(catalogDb, r2Storage.GetPublicStreamUrl);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", traceId);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";

    var problem = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "An unexpected error occurred.",
        Detail = exposeDetailedErrors ? exception?.Message : null,
        Instance = context.Request.Path,
        Extensions =
        {
            ["traceId"] = traceId,
            ["timestamp"] = DateTimeOffset.UtcNow,
        },
    };

    if (exposeDetailedErrors && exception is not null)
    {
        problem.Extensions["stackTrace"] = exception.StackTrace;
    }

    await context.Response.WriteAsJsonAsync(problem);
}));

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapApiEndpoints();
app.MapHub<ZMovie.Infrastructure.Realtime.WatchPartyHub>("/hubs/watch-party");
app.MapHub<ZMovie.Infrastructure.Realtime.DanmakuHub>("/hubs/danmaku");

app.Run();


public partial class Program
{
    private static int? ReadIntegerOption(string[] args, string optionName)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index + 1 >= args.Length) return null;
        return int.TryParse(args[index + 1], out var parsed) ? parsed : null;
    }
}
