using System.Net;
using System.Text;

namespace TiempoApp.Tests.Fakes;

/// <summary>Returns canned responses (in order; the last one repeats) and records every request.</summary>
public sealed class FakeHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders)
    : HttpMessageHandler
{
    private readonly List<HttpRequestMessage> _requests = [];

    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    public static FakeHttpMessageHandler Returning(HttpStatusCode status, string body = "") =>
        new(_ => Json(status, body));

    public static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);
        var responder = responders[Math.Min(_requests.Count, responders.Length) - 1];
        return Task.FromResult(responder(request));
    }
}
