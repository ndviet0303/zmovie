using System.Net;

namespace ZMovie.Api.Tests.Infrastructure;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> responses = new();
    public List<Uri> Requests { get; } = [];

    public FakeHttpMessageHandler Enqueue(HttpStatusCode statusCode, string? json = null, Action<HttpResponseMessage>? configure = null)
    {
        responses.Enqueue(_ => CreateResponse(statusCode, json, configure));
        return this;
    }

    public FakeHttpMessageHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        responses.Enqueue(responseFactory);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);
        if (responses.Count == 0) throw new InvalidOperationException("No fake response was queued.");
        return Task.FromResult(responses.Dequeue()(request));
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string? json, Action<HttpResponseMessage>? configure)
    {
        var response = new HttpResponseMessage(statusCode);
        if (json is not null) response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        configure?.Invoke(response);
        return response;
    }
}
