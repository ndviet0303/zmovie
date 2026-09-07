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

if (await CliCommands.TryHandleAsync(args, app.Services))
{
    return;
}

// Keep the deployed schema in lockstep with the API before accepting requests.
// Test hosts use an in-memory provider, so they intentionally skip migrations.
await using (var migrationScope = app.Services.CreateAsyncScope())
{
    var catalogDb = migrationScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    if (string.Equals(catalogDb.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        await catalogDb.Database.MigrateAsync();
        await migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await migrationScope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.MigrateAsync();
        await migrationScope.ServiceProvider.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync();
        await migrationScope.ServiceProvider.GetRequiredService<PersonalizationDbContext>().Database.MigrateAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    if (string.Equals(catalogDb.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
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
app.MapHub<ZMovie.Infrastructure.WatchParty.WatchPartyHub>("/hubs/watch-party");
app.MapHub<ZMovie.Infrastructure.Realtime.DanmakuHub>("/hubs/danmaku");

app.Run();


public partial class Program;
