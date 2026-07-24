using ZMovie.Api.Services;

namespace ZMovie.Api.Endpoints;

public static class CrawlerEndpoints
{
    public static IEndpointRouteBuilder MapCrawlerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var crawler = endpoints.MapGroup("/v1/admin/crawler").WithTags("Crawler");

        crawler.MapGet("/status", (OPhimCrawlerService service) => Results.Ok(service.GetStatus()));
        crawler.MapPost("/start", (StartCrawlerRequest request, OPhimCrawlerService service) =>
        {
            var options = new OPhimCrawlerStartOptions(request.StartPage, request.EndPage, request.IncludeEpisodes);
            if (!service.TryStart(options)) return Results.BadRequest(new { message = "Khoảng page không hợp lệ hoặc crawler đang chạy." });
            return Results.Accepted("/v1/admin/crawler/status", service.GetStatus());
        });
        crawler.MapPost("/stop", (OPhimCrawlerService service) =>
            service.TryStop() ? Results.Accepted("/v1/admin/crawler/status", service.GetStatus()) : Results.Conflict(new { message = "Crawler hiện không chạy." }));

        return endpoints;
    }
}

public sealed record StartCrawlerRequest(int StartPage = 1, int? EndPage = null, bool IncludeEpisodes = false);
