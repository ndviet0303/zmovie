using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ZMovie.Application.Common;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                logger.LogWarning("Long-running request: {RequestName} executed in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogDebug("Request {RequestName} executed in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Request {RequestName} failed after {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
