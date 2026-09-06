using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Domain.Identity;
using Xunit;

namespace ZMovie.Api.Tests.Http;

public sealed class HttpContractTests(ZMovieWebApplicationFactory factory) : IClassFixture<ZMovieWebApplicationFactory>
{
    private static readonly string[] ExpectedRoutes =
    [
        "GET /health/live|Anonymous",
        "GET /health/ready|Anonymous",
        "GET /v1/catalog/titles|Anonymous",
        "GET /v1/catalog/titles/{slug}|Anonymous",
        "GET /v1/catalog/genres|Anonymous",
        "GET /v1/catalog/schedule|Anonymous",
        "GET /v1/catalog/people|Anonymous",
        "GET /v1/catalog/people/{slug}|Anonymous",
        "GET /v1/catalog/titles/{slug}/playback|Anonymous",
        "POST /v1/catalog/titles/{slug}/views|Anonymous",
        "GET /v1/catalog/titles/{slug}/reviews|Anonymous",
        "GET /v1/watch-parties|Anonymous",
        "POST /v1/watch-parties|Anonymous",
        "DELETE /v1/watch-parties/{roomId}|Anonymous",
        "POST /v1/catalog/titles/{slug}/reports|Anonymous",
        "GET /v1/discovery/home|Anonymous",
        "GET /v1/discovery/top/{period}|Anonymous",
        "GET /v1/discovery/for-you|Authenticated",
        "GET /v1/search|Anonymous",
        "POST /v1/auth/google|Anonymous",
        "POST /v1/auth/register|Anonymous",
        "POST /v1/auth/login|Anonymous",
        "POST /v1/auth/forgot-password|Anonymous",
        "POST /v1/auth/reset-password|Anonymous",
        "GET /v1/auth/me|Authenticated",
        "POST /v1/auth/logout|Authenticated",
        "GET /v1/me/library|Authenticated",
        "PUT /v1/me/saved/{slug}|Authenticated",
        "DELETE /v1/me/saved/{slug}|Authenticated",
        "POST /v1/me/history/{slug}|Authenticated",
        "DELETE /v1/me/history/{slug}|Authenticated",
        "DELETE /v1/me/history|Authenticated",
        "PUT /v1/me/titles/{slug}/review|Authenticated",
        "DELETE /v1/me/titles/{slug}/review|Authenticated",
        "POST /v1/assistant/context|Authenticated",
        "GET /v1/assistant/context|Authenticated",
        "POST /v1/assistant/chat|Authenticated",
        "GET /v1/assistant/chat|Authenticated",
        "GET /v1/assistant/chat/stream|Authenticated",
        "POST /v1/assistant/feedback|Authenticated",
        "POST /v1/billing/checkout|Authenticated",
        "POST /v1/billing/webhook|Anonymous",
        "GET /v1/admin/overview|Admin",
        "GET /v1/admin/analytics/overview|Admin",
        "GET /v1/admin/crawler/status|Admin",
        "POST /v1/admin/crawler/sync|Admin",
        "GET /v1/admin/titles|Admin",
        "GET /v1/admin/titles/{slug}|Admin",
        "PUT /v1/admin/titles/{slug}|Admin",
        "PATCH /v1/admin/titles/{slug}/featured|Admin",
        "DELETE /v1/admin/titles/{slug}|Admin",
        "GET /v1/admin/users|Admin",
        "PATCH /v1/admin/users/{id:guid}/role|Admin",
        "GET /v1/admin/reviews|Admin",
        "DELETE /v1/admin/reviews/{id:guid}|Admin",
        "GET /v1/admin/genres|Admin",
        "POST /v1/admin/genres|Admin",
        "PUT /v1/admin/genres/{id:guid}|Admin",
        "DELETE /v1/admin/genres/{id:guid}|Admin",
    ];

    [Fact]
    public void Route_templates_and_authorization_metadata_match_the_baseline()
    {
        _ = factory.CreateContractClient();
        var expectedPatterns = ExpectedRoutes.Select(route => route[(route.IndexOf(' ') + 1)..route.IndexOf('|')]).ToHashSet();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is { } pattern && expectedPatterns.Contains(pattern));

        var actual = endpoints.SelectMany(endpoint =>
        {
            var authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var access = authorization.Any(data => data.Policy == ApiAuthorizationPolicies.AdminPolicy)
                ? "Admin"
                : authorization.Count > 0 ? "Authenticated" : "Anonymous";
            return endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods
                .Select(method => $"{method} {endpoint.RoutePattern.RawText}|{access}");
        }).Order().ToArray();

        Assert.Equal(ExpectedRoutes.Order().ToArray(), actual);
        Assert.Equal(24, actual.Count(route => route.EndsWith("|Anonymous", StringComparison.Ordinal)));
        Assert.Equal(18, actual.Count(route => route.EndsWith("|Authenticated", StringComparison.Ordinal)));
        Assert.Equal(17, actual.Count(route => route.EndsWith("|Admin", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Public_endpoint_groups_preserve_success_error_and_analytics_cookie_contracts()
    {
        using var client = factory.CreateContractClient();

        var live = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        await AssertJsonAsync(live, """{"status":"live"}""");

        var catalog = await client.GetAsync("/v1/catalog/titles?locale=en");
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        await AssertJsonAsync(catalog, """
            {"items":[{"slug":"baseline-title","title":"Baseline Title","genre":"Drama","year":2026,"type":"movie","posterUrl":"https://example.test/poster.jpg","isR2Hosted":false,"country":""}],"total":1}
            """);

        var missing = await client.GetAsync("/v1/catalog/titles/missing?locale=en");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await AssertProblemAsync(missing, 404, "catalog.title.not_found", "Catalog title not found.");

        var home = await client.GetAsync("/v1/discovery/home?locale=en");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        using (var homeJson = JsonDocument.Parse(await home.Content.ReadAsStringAsync()))
        {
            Assert.Equal("baseline-title", homeJson.RootElement.GetProperty("hero").GetProperty("slug").GetString());
            Assert.Equal(1, homeJson.RootElement.GetProperty("trending").GetArrayLength());
        }

        var invalidPeriod = await client.GetAsync("/v1/discovery/top/year");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPeriod.StatusCode);
        using (var periodJson = JsonDocument.Parse(await invalidPeriod.Content.ReadAsStringAsync()))
        {
            Assert.Equal(400, periodJson.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Use day, week, or month.", periodJson.RootElement.GetProperty("errors").GetProperty("period")[0].GetString());
        }

        var search = await client.GetAsync("/v1/search?q=baseline&locale=en");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        await AssertJsonAsync(search, """
            {"items":[{"slug":"baseline-title","title":"Baseline Title","genre":"Drama","year":2026,"type":"movie","posterUrl":"https://example.test/poster.jpg","isR2Hosted":false,"country":""}],"total":1}
            """);

        var view = await client.PostAsJsonAsync("/v1/catalog/titles/baseline-title/views", new { episodeNumber = (int?)null });
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);
        await AssertJsonAsync(view, """{"viewCount":1,"counted":true}""");
        var analyticsCookie = Assert.Single(view.Headers.GetValues("Set-Cookie"));
        Assert.Contains("zmovie.analytics-session=", analyticsCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", analyticsCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", analyticsCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=2592000", analyticsCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Authentication_cookie_drives_auth_engagement_assistant_and_logout_contracts()
    {
        using var client = factory.CreateContractClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/auth/me")).StatusCode);

        var signIn = await SignInAsync(client, ZMovieWebApplicationFactory.MemberCredential);
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        await AssertJsonAsync(signIn, $$"""
            {"id":"{{ZMovieWebApplicationFactory.MemberId}}","email":"member@zmovie.test","displayName":"Contract Member","avatarUrl":"https://example.test/member.jpg","role":"member"}
            """);
        var sessionCookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"));
        Assert.Contains("zmovie.session=", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", sessionCookie, StringComparison.OrdinalIgnoreCase);

        var me = await client.GetAsync("/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        await AssertJsonAsync(me, $$"""
            {"id":"{{ZMovieWebApplicationFactory.MemberId}}","email":"member@zmovie.test","displayName":"Contract Member","avatarUrl":"https://example.test/member.jpg","role":"member"}
            """);

        var library = await client.GetAsync("/v1/me/library?locale=en");
        Assert.Equal(HttpStatusCode.OK, library.StatusCode);
        await AssertJsonAsync(library, """{"saved":[],"history":[]}""");

        var assistant = await client.GetAsync("/v1/assistant/context?message=baseline&locale=en");
        Assert.Equal(HttpStatusCode.OK, assistant.StatusCode);
        using (var assistantJson = JsonDocument.Parse(await assistant.Content.ReadAsStringAsync()))
        {
            Assert.Equal("baseline-title", assistantJson.RootElement.GetProperty("matches")[0].GetProperty("title").GetProperty("slug").GetString());
            Assert.Equal(ZMovieWebApplicationFactory.RecommendationId, assistantJson.RootElement.GetProperty("recommendationId").GetGuid());
        }

        var invalidAssistant = await client.PostAsJsonAsync("/v1/assistant/chat", new { message = "", locale = "en" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidAssistant.StatusCode);
        await AssertProblemAsync(invalidAssistant, 400, "Message", "'Message' must not be empty.");

        var logout = await client.PostAsync("/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), header =>
            header.Contains("zmovie.session=;", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_group_preserves_unauthorized_forbidden_success_and_not_found_contracts()
    {
        using var anonymous = factory.CreateContractClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/v1/admin/genres")).StatusCode);

        using var member = factory.CreateContractClient();
        _ = await SignInAsync(member, ZMovieWebApplicationFactory.MemberCredential);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/v1/admin/genres")).StatusCode);

        using var admin = factory.CreateContractClient();
        _ = await SignInAsync(admin, ZMovieWebApplicationFactory.AdminCredential);
        var genres = await admin.GetAsync("/v1/admin/genres");
        Assert.Equal(HttpStatusCode.OK, genres.StatusCode);
        using (var genreJson = JsonDocument.Parse(await genres.Content.ReadAsStringAsync()))
        {
            var genre = Assert.Single(genreJson.RootElement.EnumerateArray());
            Assert.Equal(ZMovieWebApplicationFactory.GenreId, genre.GetProperty("id").GetGuid());
            Assert.Equal("drama", genre.GetProperty("slug").GetString());
            Assert.Equal("Drama", genre.GetProperty("name").GetString());
            Assert.Equal(1, genre.GetProperty("titleCount").GetInt32());
        }

        var missing = await admin.GetAsync("/v1/admin/titles/missing");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await AssertProblemAsync(missing, 404, "admin.title.not_found", "Catalog title not found.");
    }

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string credential) =>
        client.PostAsJsonAsync("/v1/auth/google", new { credential });

    private static async Task AssertJsonAsync(HttpResponseMessage response, string expected)
    {
        var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), actual), $"Expected: {expected}{Environment.NewLine}Actual: {actual}");
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, int status, string code, string title)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(code, json.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }
}
