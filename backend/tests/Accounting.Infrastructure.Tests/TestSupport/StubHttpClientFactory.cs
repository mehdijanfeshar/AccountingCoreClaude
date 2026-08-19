namespace Accounting.Infrastructure.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IHttpClientFactory"/> stand-in that always hands out an
/// <see cref="HttpClient"/> wired to the given <see cref="HttpMessageHandler"/>, regardless of
/// the requested client name — enough to prove TokenManager goes through the factory
/// (defect fix #2) instead of constructing its own <see cref="HttpClient"/>.
/// </summary>
internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public List<string> RequestedClientNames { get; } = new();

    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        RequestedClientNames.Add(name);
        return new HttpClient(_handler, disposeHandler: false);
    }
}
