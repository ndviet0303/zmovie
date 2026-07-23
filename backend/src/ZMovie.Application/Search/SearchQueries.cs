using ErrorOr;
using MediatR;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;

namespace ZMovie.Application.Search;

public sealed record SearchCatalogQuery(string? Query, string? Type, string? Genre, string? Locale) : IQuery<TitleListResponse>;
public interface ISearchCatalogStore { Task<TitleListResponse> SearchAsync(string query, string? type, string? genre, string locale, CancellationToken ct); }
public sealed class SearchCatalogHandler(ISearchCatalogStore store) : IRequestHandler<SearchCatalogQuery, ErrorOr<TitleListResponse>>
{ public async Task<ErrorOr<TitleListResponse>> Handle(SearchCatalogQuery request, CancellationToken ct) => await store.SearchAsync(request.Query?.Trim() ?? string.Empty, request.Type, request.Genre, Locale.Normalize(request.Locale), ct); }
