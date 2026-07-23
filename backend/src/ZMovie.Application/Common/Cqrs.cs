using ErrorOr;
using MediatR;

namespace ZMovie.Application.Common;

public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>>;
public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>>;

public static class Locale
{
    public static string Normalize(string? locale) => locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) is true ? "en" : "vi";
}
