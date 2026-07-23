using MediatR;
using ZMovie.Api;
using ZMovie.Application.Catalog;
using ZMovie.Application.Search;

namespace ZMovie.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/v1/search", async (ISender sender, string? q, string? type, string? genre, string? locale, CancellationToken ct) =>
                (await sender.Send(new SearchCatalogQuery(q, type, genre, locale), ct)).ToApiResult())
            .WithName("SearchCatalog")
            .Produces<TitleListResponse>(StatusCodes.Status200OK)
            .ProducesApiErrors();

        return endpoints;
    }
}
