namespace Accounting.Infrastructure.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="HttpMessageHandler"/> stand-in so <see cref="Idp.TokenManagerTests"/>
/// never makes a real network call. Records every request body it receives (for asserting what
/// TokenManager actually POSTs) and lets each test script an arbitrary sequence of responses.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _responder;

    public List<string?> RequestBodies { get; } = new();

    public int RequestCount { get; private set; }

    public StubHttpMessageHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        RequestBodies.Add(body);
        return _responder(request, body);
    }
}
