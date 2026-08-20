using System.Net;
using System.Text;
using Accounting.Infrastructure.Idp;
using Accounting.Infrastructure.Tests.TestSupport;

namespace Accounting.Infrastructure.Tests.Idp;

/// <summary>
/// Unit/mock tests only — no real network call, no live IDP, per CLAUDE.md task constraints.
/// <see cref="StubHttpMessageHandler"/>/<see cref="StubHttpClientFactory"/> stand in for the real
/// HTTP pipeline.
/// </summary>
public sealed class TokenManagerTests
{
    private const string TokenKey = "tamin";

    private static TokenManagerDetail ValidDetail(string clientSecret = "super-secret-value") => new()
    {
        Server = "https://idp.example.test/connect/token",
        ClientId = "client-id",
        ClientSecret = clientSecret,
        Audience = "aud",
        GrantType = "client_credentials",
        Timeout = TimeSpan.FromSeconds(5),
        Resources = new List<string> { "resource-a", "resource-b" },
    };

    private static TokenManagerConfiguration ConfigWith(string tokenKey, TokenManagerDetail detail)
    {
        var configuration = new TokenManagerConfiguration();
        configuration.TokenManagers[tokenKey] = detail;
        return configuration;
    }

    private static HttpResponseMessage JsonTokenResponse(string accessToken, int expiresIn)
    {
        var json = $$"""{"access_token":"{{accessToken}}","expires_in":{{expiresIn}},"token_type":"Bearer"}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    [Fact]
    public void Constructor_DoesNotThrow_WhenConfigurationIsEmpty()
    {
        // Lazy validation: an API with no Idp:* config configured must still be able to start
        // (register ITokenManager) without crashing. Only the first GetAccessTokenAsync call for
        // an unconfigured/incomplete key should fail.
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token", 3600));
        var factory = new StubHttpClientFactory(handler);

        var exception = Record.Exception(
            () => new TokenManager(factory, new TokenManagerConfiguration(), new RecordingLogger<TokenManager>()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReusesCachedToken_WhileStillValid()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token-1", expiresIn: 3600));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        var first = await manager.GetAccessTokenAsync(TokenKey);
        var second = await manager.GetAccessTokenAsync(TokenKey);

        Assert.Equal("token-1", first);
        Assert.Equal("token-1", second);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshesOnce_ExpiryDeadlineHasPassed()
    {
        // expires_in is set exactly at the ExpiryMarginSeconds boundary so the token is already
        // considered expired the instant it is cached (margin >= expires_in) — this proves the
        // refresh path runs again without depending on a real-time sleep/flaky timing window.
        var callCount = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            return JsonTokenResponse($"token-{callCount}", expiresIn: TokenManager.ExpiryMarginSeconds);
        });
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        var first = await manager.GetAccessTokenAsync(TokenKey);
        var second = await manager.GetAccessTokenAsync(TokenKey);

        Assert.Equal("token-1", first);
        Assert.Equal("token-2", second);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshesAfterRealExpiry()
    {
        // Complements the deterministic test above with one real-clock expiry: expires_in leaves
        // ~1s of real cache validity (expires_in - ExpiryMarginSeconds), then we wait past it.
        var callCount = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            return JsonTokenResponse($"token-{callCount}", expiresIn: TokenManager.ExpiryMarginSeconds + 1);
        });
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        var first = await manager.GetAccessTokenAsync(TokenKey);
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        var second = await manager.GetAccessTokenAsync(TokenKey);

        Assert.Equal("token-1", first);
        Assert.Equal("token-2", second);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_PropagatesFailure_WhenIdpReturnsErrorStatus()
    {
        var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        await Assert.ThrowsAsync<TokenAcquisitionException>(() => manager.GetAccessTokenAsync(TokenKey));
    }

    [Fact]
    public async Task GetAccessTokenAsync_PropagatesFailure_WhenHttpRequestThrows()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("connection refused"));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        var exception = await Assert.ThrowsAsync<TokenAcquisitionException>(() => manager.GetAccessTokenAsync(TokenKey));
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task GetAccessTokenAsync_PropagatesFailure_WhenResponseBodyIsNotValidJson()
    {
        var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json"),
        });
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        await Assert.ThrowsAsync<TokenAcquisitionException>(() => manager.GetAccessTokenAsync(TokenKey));
    }

    [Fact]
    public async Task GetAccessTokenAsync_PropagatesFailure_WhenAccessTokenIsMissing()
    {
        var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"expires_in":3600}""", Encoding.UTF8, "application/json"),
        });
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        await Assert.ThrowsAsync<TokenAcquisitionException>(() => manager.GetAccessTokenAsync(TokenKey));
    }

    [Fact]
    public async Task GetAccessTokenAsync_DoesNotReturnStaleToken_WhenRefreshFailsAfterPriorSuccessExpired()
    {
        // The core "never return a stale/null token after a failed refresh" guarantee: a
        // successful token is cached, it expires, and the next refresh attempt fails outright.
        var callCount = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            return callCount == 1
                ? JsonTokenResponse("token-1", expiresIn: TokenManager.ExpiryMarginSeconds)
                : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        var first = await manager.GetAccessTokenAsync(TokenKey);
        Assert.Equal("token-1", first);

        await Assert.ThrowsAsync<TokenAcquisitionException>(() => manager.GetAccessTokenAsync(TokenKey));
    }

    [Fact]
    public async Task GetAccessTokenAsync_UnknownTokenKey_ThrowsInvalidOperationException()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token", 3600));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, new TokenManagerConfiguration(), new RecordingLogger<TokenManager>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.GetAccessTokenAsync("does-not-exist"));

        Assert.Contains("does-not-exist", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_IncompleteConfiguration_ThrowsListingFieldNames_NeverValues()
    {
        const string secret = "must-never-appear-anywhere";
        var detail = new TokenManagerDetail
        {
            Server = "https://idp.example.test/connect/token",
            ClientId = "client-id",
            ClientSecret = string.Empty, // missing on purpose
            GrantType = "client_credentials",
        };
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token", 3600));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, detail), new RecordingLogger<TokenManager>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetAccessTokenAsync(TokenKey));

        Assert.Contains(nameof(TokenManagerDetail.ClientSecret), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_UsesInjectedHttpClientFactory_NotANewHttpClientPerCall()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token-1", expiresIn: 3600));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail()), new RecordingLogger<TokenManager>());

        await manager.GetAccessTokenAsync(TokenKey);

        Assert.Contains(TokenManager.HttpClientName, factory.RequestedClientNames);
    }

    [Fact]
    public async Task GetAccessTokenAsync_SendsExpectedFormFields_AndNeverSendsResourcesOrToken()
    {
        var detail = ValidDetail();
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token-1", expiresIn: 3600));
        var factory = new StubHttpClientFactory(handler);
        var manager = new TokenManager(factory, ConfigWith(TokenKey, detail), new RecordingLogger<TokenManager>());

        await manager.GetAccessTokenAsync(TokenKey);

        var body = Assert.Single(handler.RequestBodies);
        Assert.NotNull(body);
        var form = ParseFormBody(body!);
        Assert.Equal(detail.GrantType, form["grant_type"]);
        Assert.Equal(detail.ClientId, form["client_id"]);
        Assert.Equal(detail.ClientSecret, form["client_secret"]);
        Assert.Equal(detail.Audience, form["audience"]);

        // Defect #7: Resources is configured (see ValidDetail) but must stay unsent, exactly
        // matching the ported pattern's behaviour, pending owner confirmation.
        Assert.False(form.ContainsKey("resource"));
        Assert.False(form.ContainsKey("resources"));
        Assert.DoesNotContain("resource-a", body, StringComparison.Ordinal);
        Assert.DoesNotContain("resource-b", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Minimal application/x-www-form-urlencoded parser — avoids taking a dependency on
    /// System.Web/ASP.NET WebUtilities from a plain test project just to assert form fields.
    /// </summary>
    private static Dictionary<string, string> ParseFormBody(string body)
    {
        return body
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NeverLogsClientSecretOrAccessToken_OnSuccess()
    {
        const string secret = "super-secret-value-xyz";
        const string accessToken = "eyJhbGciOiJSUzI1NiJ9.secret-token-payload";
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse(accessToken, expiresIn: 3600));
        var factory = new StubHttpClientFactory(handler);
        var logger = new RecordingLogger<TokenManager>();
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail(secret)), logger);

        await manager.GetAccessTokenAsync(TokenKey);

        Assert.DoesNotContain(logger.Messages, m => m.Contains(secret, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, m => m.Contains(accessToken, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAccessTokenAsync_NeverLogsClientSecret_OnFailure()
    {
        const string secret = "super-secret-value-xyz";
        var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var factory = new StubHttpClientFactory(handler);
        var logger = new RecordingLogger<TokenManager>();
        var manager = new TokenManager(factory, ConfigWith(TokenKey, ValidDetail(secret)), logger);

        var exception = await Assert.ThrowsAsync<TokenAcquisitionException>(() => manager.GetAccessTokenAsync(TokenKey));

        Assert.DoesNotContain(logger.Messages, m => m.Contains(secret, StringComparison.Ordinal));
        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAccessTokenAsync_AppliesConfiguredTimeout_ToTheHttpClient()
    {
        HttpClient? capturedClient = null;
        var handler = new StubHttpMessageHandler((_, _) => JsonTokenResponse("token-1", expiresIn: 3600));

        // A thin wrapping factory that captures the client TokenManager actually used, to assert
        // detail.Timeout (defect fix #2) was applied to it.
        var innerFactory = new StubHttpClientFactory(handler);
        var capturingFactory = new CapturingHttpClientFactory(innerFactory, client => capturedClient = client);
        var detail = ValidDetail();
        detail.Timeout = TimeSpan.FromSeconds(7);
        var manager = new TokenManager(capturingFactory, ConfigWith(TokenKey, detail), new RecordingLogger<TokenManager>());

        await manager.GetAccessTokenAsync(TokenKey);

        Assert.NotNull(capturedClient);
        Assert.Equal(TimeSpan.FromSeconds(7), capturedClient!.Timeout);
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        private readonly IHttpClientFactory _inner;
        private readonly Action<HttpClient> _onCreated;

        public CapturingHttpClientFactory(IHttpClientFactory inner, Action<HttpClient> onCreated)
        {
            _inner = inner;
            _onCreated = onCreated;
        }

        public HttpClient CreateClient(string name)
        {
            var client = _inner.CreateClient(name);
            _onCreated(client);
            return client;
        }
    }
}
