using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ZMovie.Api.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ZMovie.Api;
using ZMovie.Api.Endpoints;
using ZMovie.Application.Catalog;
using ZMovie.Application.Assistant;
using ZMovie.Application.Common;
using ZMovie.Application.Engagement;
using ZMovie.Application.Identity;
using ZMovie.Application.Search;
using ZMovie.Infrastructure.Catalog;
using ZMovie.Infrastructure.Engagement;
using ZMovie.Infrastructure.Identity;
using ZMovie.Infrastructure.Persistence;
using ZMovie.Infrastructure.Search;
using ZMovie.Infrastructure.Seed;
using ZMovie.Infrastructure.Recommendations;
using ZMovie.Infrastructure.Assistant;
using ZMovie.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var exposeDetailedErrors = builder.Configuration.GetValue<bool>("ExposeDetailedErrors");

await builder.Configuration.AddInfisicalSecretsAsync(builder.Environment);
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "zmovie.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("ZMovie")
    ?? throw new InvalidOperationException("ConnectionStrings:ZMovie must be configured.")).UseSnakeCaseNamingConvention());
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ListTitlesQuery).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(ListTitlesQuery).Assembly);
builder.Services.AddScoped<ICatalogReadStore, EfCatalogReadStore>();
builder.Services.AddHttpClient<ISearchCatalogStore, SearchCatalogStore>();
builder.Services.AddScoped<IUserIdentityStore, EfUserIdentityStore>();
builder.Services.AddScoped<IGoogleIdentityVerifier, GoogleIdentityVerifier>();
builder.Services.AddScoped<EfUserLibraryStore>();
builder.Services.AddScoped<IUserLibraryStore>(provider => provider.GetRequiredService<EfUserLibraryStore>());
builder.Services.AddScoped<IViewAnalyticsStore, CachedViewAnalyticsStore>();
builder.Services.AddScoped<ITitleReviewStore>(provider => provider.GetRequiredService<EfUserLibraryStore>());
builder.Services.AddSingleton<ITopTitlesResponseCache, TopTitlesResponseCache>();
builder.Services.AddSingleton<IRecommendationEngine, TinyContentRecommendationEngine>();
builder.Services.AddScoped<ILibraryCatalogReader, CatalogLibraryReader>();
builder.Services.AddScoped<ICatalogAssistantStore, CatalogAssistantStore>();
builder.Services.Configure<LocalAiOptions>(builder.Configuration.GetSection("LocalAi"));
builder.Services.AddHttpClient<IAssistantTextGenerator, LocalAiAssistantTextGenerator>((serviceProvider, http) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<LocalAiOptions>>().Value;
    http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    http.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 60));
});
builder.Services.AddSingleton<OPhimCrawlerService>();

var app = builder.Build();

if (args.Contains("--import-ophim-genres", StringComparer.OrdinalIgnoreCase))
{
    await using var importScope = app.Services.CreateAsyncScope();
    var importDb = importScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await importDb.Database.MigrateAsync();
    var imported = await OPhimGenreImporter.ImportAsync(importDb, new HttpClient(), CancellationToken.None);
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
    await importDb.Database.MigrateAsync();
    var options = new OPhimCatalogImportOptions(maxPages, startPage, includeEpisodes, TimeSpan.FromMilliseconds(300))
    {
        DetailConcurrency = detailConcurrency,
    };
    var imported = await OPhimCatalogImporter.ImportAsync(importDb, new HttpClient(), options, Console.WriteLine, CancellationToken.None);
    Console.WriteLine($"Imported {imported.TitlesImported} OPhim titles from {imported.PagesImported} pages (source total: {imported.TotalItems}; episodes: {imported.EpisodesImported}).");
    return;
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await catalogDb.Database.MigrateAsync();
    await CatalogSeed.SeedAsync(catalogDb);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapCrawlerEndpoints();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", traceId);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";
    var response = new Dictionary<string, object?>
    {
        ["type"] = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        ["title"] = "An error occurred while processing your request.",
        ["status"] = 500,
        ["traceId"] = traceId,
    };
    if (exposeDetailedErrors) response["detail"] = exception?.GetBaseException().Message;
    await context.Response.WriteAsJsonAsync(response);
}));
// app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapApiEndpoints();

static int? ReadIntegerOption(string[] arguments, string name)
{
    var index = Array.FindIndex(arguments, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < arguments.Length && int.TryParse(arguments[index + 1], out var result) && result > 0 ? result : null;
}

app.Run();

public partial class Program;
