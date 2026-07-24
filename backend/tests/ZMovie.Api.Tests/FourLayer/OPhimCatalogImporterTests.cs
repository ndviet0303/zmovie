using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Domain.Catalog;
using ZMovie.Infrastructure.Catalog;
using Xunit;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class OPhimCatalogImporterTests
{
    [Fact]
    public async Task Imports_pages_deduplicates_slugs_and_reports_progress()
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, ListJson(3, 2,
                Movie("dup", "Tên 1", "Origin 1"),
                Movie("DUP", "Tên trùng", "Origin trùng"),
                Movie("", "Bỏ qua", "Bỏ qua")))
            .Enqueue(HttpStatusCode.OK, ListJson(3, 2, Movie("second", "Tên 2", "Origin 2")));
        using var http = new HttpClient(handler);
        var reports = new List<string>();

        var result = await OPhimCatalogImporter.ImportAsync(
            database.Db,
            http,
            new OPhimCatalogImportOptions(null, 1, false, TimeSpan.Zero),
            reports.Add,
            CancellationToken.None);

        result.Should().Be(new OPhimCatalogImportResult(3, 2, 2, 0));
        database.Db.Titles.Should().HaveCount(2);
        reports.Should().ContainInOrder(
            "OPhim catalog: page 1/2 (1 titles, 0 episodes)",
            "OPhim catalog: page 2/2 (2 titles, 0 episodes)");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[1].Query.Should().Contain("page=2");
    }

    [Fact]
    public async Task Upserts_metadata_and_episodes_with_fallbacks()
    {
        using var database = new TestDatabase();
        var existing = new CatalogTitle
        {
            Slug = "existing",
            EnglishTitle = "old",
            VietnameseTitle = "old",
            EnglishSynopsis = "already translated",
            VietnameseSynopsis = "old synopsis",
            Genre = "Old",
            Year = 2000,
            Type = "movie",
            PosterUrl = "old",
            RuntimeMinutes = 1
        };
        database.Db.Titles.Add(existing);
        await database.Db.SaveChangesAsync();

        var tooLong = new string('x', 2_001);
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, ListJson(1, 20, Movie("existing", "  Tên mới  ", "Origin mới", "single", 2026, "2 giờ 5 phút", tooLong, "fallback.jpg", ["Drama", " "])))
            .Enqueue(HttpStatusCode.OK, DetailJson("<p>Nội dung <b>mới</b></p>", [
                Server(("1", "https://video/one.m3u8"), ("2", null)),
                Server((null, "https://video/two.m3u8"))]));
        using var http = new HttpClient(handler);

        var result = await OPhimCatalogImporter.ImportAsync(
            database.Db,
            http,
            new OPhimCatalogImportOptions(1, 1, true, TimeSpan.Zero),
            null,
            CancellationToken.None);

        result.Should().Be(new OPhimCatalogImportResult(1, 1, 1, 2));
        var title = await database.Db.Titles.SingleAsync();
        title.Id.Should().Be(existing.Id);
        title.EnglishTitle.Should().Be("Origin mới");
        title.VietnameseTitle.Should().Be("Tên mới");
        title.Genre.Should().Be("Drama");
        title.Type.Should().Be("movie");
        title.RuntimeMinutes.Should().Be(2);
        title.PosterUrl.Should().Be("https://cdn.test/uploads/movies/fallback.jpg");
        title.VietnameseSynopsis.Should().Be("Nội dung  mới");
        title.EnglishSynopsis.Should().Be("already translated");
        var episodes = await database.Db.Episodes.OrderBy(x => x.Number).ToListAsync();
        episodes.Select(x => (x.Number, x.Name, x.HlsUrl)).Should().Equal(
            (1, "1", "https://video/one.m3u8"),
            (2, "Tập 2", "https://video/two.m3u8"));

        handler.Enqueue(HttpStatusCode.OK, ListJson(1, 20, Movie("existing", "Tên mới", "Origin mới")))
            .Enqueue(HttpStatusCode.OK, DetailJson("updated", [Server(("1", "https://video/updated.m3u8"))]));
        var secondResult = await OPhimCatalogImporter.ImportAsync(
            database.Db, http, new OPhimCatalogImportOptions(1, 1, true, TimeSpan.FromMilliseconds(1)), null, CancellationToken.None);
        secondResult.EpisodesImported.Should().Be(1);
        (await database.Db.Episodes.SingleAsync(x => x.Number == 1)).HlsUrl.Should().Be("https://video/updated.m3u8");
    }

    [Fact]
    public async Task Retries_transient_responses_and_honors_retry_after()
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.TooManyRequests, configure: response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1)))
            .Enqueue(HttpStatusCode.OK, ListJson(0, 20));
        using var http = new HttpClient(handler);

        var result = await OPhimCatalogImporter.ImportAsync(
            database.Db, http, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None);

        result.Should().Be(new OPhimCatalogImportResult(0, 0, 0, 0));
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Retries_request_exceptions_and_exposes_full_metadata_defaults()
    {
        OPhimCatalogImportOptions.FullMetadata.Should().Be(new OPhimCatalogImportOptions(null, 1, false, TimeSpan.FromMilliseconds(300)));
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(_ => throw new HttpRequestException("temporary"))
            .Enqueue(HttpStatusCode.OK, ListJson(0, 20));
        using var http = new HttpClient(handler);

        (await OPhimCatalogImporter.ImportAsync(database.Db, http, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None))
            .TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task Throws_after_all_request_retries_are_exhausted()
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(_ => throw new HttpRequestException())
            .Enqueue(_ => throw new HttpRequestException())
            .Enqueue(_ => throw new HttpRequestException())
            .Enqueue(_ => throw new HttpRequestException());
        using var http = new HttpClient(handler);

        var action = () => OPhimCatalogImporter.ImportAsync(database.Db, http, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("OPhim request failed after retries.");
    }

    [Fact]
    public async Task Throws_for_a_final_transient_http_response()
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.ServiceUnavailable, configure: response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1)))
            .Enqueue(HttpStatusCode.ServiceUnavailable, configure: response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1)))
            .Enqueue(HttpStatusCode.ServiceUnavailable, configure: response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1)))
            .Enqueue(HttpStatusCode.ServiceUnavailable, configure: response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1)));
        using var http = new HttpClient(handler);

        var action = () => OPhimCatalogImporter.ImportAsync(database.Db, http, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None);
        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Throws_without_retrying_non_transient_list_errors(HttpStatusCode statusCode)
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler().Enqueue(statusCode);
        using var http = new HttpClient(handler);

        var action = () => OPhimCatalogImporter.ImportAsync(
            database.Db, http, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task Throws_when_OPhim_returns_failure_status_or_empty_payload()
    {
        using var database = new TestDatabase();
        using var failedHttp = new HttpClient(new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, ListJsonWithStatus("error", "bad")));
        var failed = () => OPhimCatalogImporter.ImportAsync(database.Db, failedHttp, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None);
        await failed.Should().ThrowAsync<InvalidOperationException>().WithMessage("OPhim request failed: bad");

        using var emptyHttp = new HttpClient(new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "null"));
        var empty = () => OPhimCatalogImporter.ImportAsync(database.Db, emptyHttp, new OPhimCatalogImportOptions(1, 1, false, TimeSpan.Zero), null, CancellationToken.None);
        await empty.Should().ThrowAsync<InvalidOperationException>().WithMessage("OPhim returned no JSON payload.");
    }

    [Fact]
    public async Task Throws_when_a_detail_request_fails()
    {
        using var database = new TestDatabase();
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, ListJson(1, 20, Movie("one", "Một", "One")))
            .Enqueue(HttpStatusCode.OK, DetailJsonWithStatus("error", "detail failed"));
        using var http = new HttpClient(handler);

        var action = () => OPhimCatalogImporter.ImportAsync(
            database.Db, http, new OPhimCatalogImportOptions(1, 1, true, TimeSpan.Zero), null, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("OPhim request failed: detail failed");
    }

    private static object Movie(string slug, string name, string originName, string type = "series", int year = 2026, string time = "24 phút", string? poster = "poster.jpg", string? thumb = "thumb.jpg", string[]? categories = null) => new
    {
        slug,
        name,
        origin_name = originName,
        type,
        year,
        time,
        poster_url = poster,
        thumb_url = thumb,
        category = (categories ?? ["Animation"]).Select(x => new { name = x }).ToArray()
    };

    private static object Server(params (string? Name, string? Link)[] episodes) => new
    {
        server_data = episodes.Select(x => new { name = x.Name, link_m3u8 = x.Link }).ToArray()
    };

    private static string ListJson(int totalItems, int perPage, params object[] movies) => JsonSerializer.Serialize(new
    {
        status = "success",
        message = (string?)null,
        data = new { @params = new { pagination = new { totalItems, totalItemsPerPage = perPage } }, items = movies, APP_DOMAIN_CDN_IMAGE = "https://cdn.test" }
    }).Replace("\"@params\"", "\"params\"", StringComparison.Ordinal);

    private static string ListJsonWithStatus(string status, string message) => JsonSerializer.Serialize(new
    {
        status,
        message,
        data = new { @params = new { pagination = new { totalItems = 0, totalItemsPerPage = 20 } }, items = Array.Empty<object>() }
    }).Replace("\"@params\"", "\"params\"", StringComparison.Ordinal);

    private static string DetailJson(string content, object[] servers) => JsonSerializer.Serialize(new
    {
        status = "success",
        message = (string?)null,
        data = new { item = new { content, episodes = servers } }
    });

    private static string DetailJsonWithStatus(string status, string message) => JsonSerializer.Serialize(new
    {
        status,
        message,
        data = new { item = new { content = (string?)null, episodes = Array.Empty<object>() } }
    });
}
