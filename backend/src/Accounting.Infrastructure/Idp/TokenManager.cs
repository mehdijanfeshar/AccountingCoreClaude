using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Default <see cref="ITokenManager"/>. Registered as a singleton by
/// <c>AddTaminTokenManager</c>; caches one access token per <c>tokenKey</c> and refreshes it
/// once it is within <see cref="ExpiryMarginSeconds"/> seconds of expiring.
///
/// This is a from-scratch port of the project owner's original <c>IDP.Services.TokenManager</c>
/// pattern (used by another project at the same organization), with the following defects fixed
/// rather than copied through — see CLAUDE.md task notes for the full defect list:
/// <list type="number">
/// <item><description>Failures propagate as <see cref="TokenAcquisitionException"/> instead of
/// being swallowed and logged to <see cref="Console"/>. A failed refresh can never leave a
/// caller holding a stale or null token.</description></item>
/// <item><description>HTTP calls go through the injected <see cref="IHttpClientFactory"/>
/// (named client <see cref="HttpClientName"/>) instead of <c>new HttpClient()</c> per refresh,
/// and <see cref="TokenManagerDetail.Timeout"/> is actually applied.</description></item>
/// <item><description><see cref="_refreshLock"/> is an instance field, not <c>static</c> — this
/// type is registered as a singleton itself, so a static lock added nothing but the confusing
/// implication that instances share a lock.</description></item>
/// <item><description>The token cache is a <see cref="ConcurrentDictionary{TKey,TValue}"/>, safe
/// for concurrent read (cache hit, no lock) and write (refresh, under <see cref="_refreshLock"/>)
/// from multiple in-flight requests.</description></item>
/// <item><description>All expiry bookkeeping uses <see cref="DateTime.UtcNow"/>, never
/// <c>DateTime.Now</c>.</description></item>
/// <item><description>No log statement includes <see cref="TokenManagerDetail.ClientSecret"/> or
/// a raw access token — only the token key, HTTP status code, and expiry timestamps are
/// logged.</description></item>
/// <item><description><see cref="TokenManagerDetail.Resources"/> remains configured but is
/// deliberately NOT sent in the token request, exactly as before — see the request-building
/// comment in <see cref="RefreshAsync"/>.</description></item>
/// </list>
/// </summary>
public sealed class TokenManager : ITokenManager, IDisposable
{
    /// <summary>
    /// Name of the named <see cref="HttpClient"/> registered by <c>AddTaminTokenManager</c>.
    /// Public (rather than internal) so unit tests in <c>Accounting.Infrastructure.Tests</c> can
    /// assert TokenManager actually goes through <see cref="IHttpClientFactory"/> with this name.
    /// </summary>
    public const string HttpClientName = "Accounting.Infrastructure.Idp.TokenManager";

    /// <summary>
    /// A token is treated as expired this many seconds before its actual <c>expires_in</c>
    /// deadline, so an in-flight request never presents a token that expires mid-call.
    /// </summary>
    public const int ExpiryMarginSeconds = 30;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenManagerConfiguration _configuration;
    private readonly ILogger<TokenManager> _logger;
    private readonly ConcurrentDictionary<string, TokenModel> _cache = new();

    // Instance field (defect fix #3) — the ported pattern used `static readonly SemaphoreSlim`
    // on a type that is *also* registered as a singleton, which achieves nothing but implying
    // (incorrectly) that separate TokenManager instances would share a lock.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public TokenManager(
        IHttpClientFactory httpClientFactory,
        TokenManagerConfiguration configuration,
        ILogger<TokenManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(string tokenKey, CancellationToken cancellationToken = default)
    {
        if (!_configuration.TokenManagers.TryGetValue(tokenKey, out var detail))
        {
            throw new InvalidOperationException(
                $"No IDP token manager is configured for key '{tokenKey}'. Configure it under " +
                $"\"Idp:{tokenKey}\" in appsettings.json / User Secrets before requesting a token " +
                "for this key.");
        }

        if (_cache.TryGetValue(tokenKey, out var cached) && cached.ExpireTimeUtc > DateTime.UtcNow)
        {
            return cached.TokenResponse.AccessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check after acquiring the lock: another caller may have already refreshed this
            // key while we were waiting, in which case there is no need to refresh again.
            if (_cache.TryGetValue(tokenKey, out cached) && cached.ExpireTimeUtc > DateTime.UtcNow)
            {
                return cached.TokenResponse.AccessToken;
            }

            var refreshed = await RefreshAsync(tokenKey, detail, cancellationToken);
            _cache[tokenKey] = refreshed;
            return refreshed.TokenResponse.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<TokenModel> RefreshAsync(
        string tokenKey,
        TokenManagerDetail detail,
        CancellationToken cancellationToken)
    {
        // Lazy validation (per CLAUDE.md task notes): Idp:* config is allowed to be empty at
        // startup so the API can still run locally when nothing calls a downstream tamin service
        // yet. The first actual GetAccessTokenAsync call for an incompletely-configured key is
        // where this surfaces, with an actionable message — never a raw NullReferenceException
        // or an opaque HTTP failure against an empty URL.
        ValidateDetail(tokenKey, detail);

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.Timeout = detail.Timeout;

        // NOTE: detail.Resources (bound from Idp:<key>:External_Resources) is intentionally NOT
        // included here. The ported pattern configured this value but never sent it either; we
        // keep that behaviour as-is rather than guessing at a `resource` parameter the IDP may or
        // may not expect. See TokenManagerDetail.Resources XML doc.
        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = detail.GrantType,
            ["client_id"] = detail.ClientId,
            ["client_secret"] = detail.ClientSecret,
            ["audience"] = detail.Audience ?? string.Empty,
        });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(detail.Server, requestBody, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            // Defect fix #1: propagate instead of swallowing. Message/log deliberately omit
            // detail.ClientSecret and detail.Server's query string (there is none here, but this
            // stays defensive) — only the token key is logged.
            _logger.LogError(ex, "Failed to reach the IDP token endpoint for token key '{TokenKey}'.", tokenKey);
            throw new TokenAcquisitionException(
                $"Failed to reach the IDP token endpoint for token key '{tokenKey}'.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "IDP token request for token key '{TokenKey}' failed with status code {StatusCode}.",
                tokenKey,
                (int)response.StatusCode);
            throw new TokenAcquisitionException(
                $"IDP token request for token key '{tokenKey}' failed with status code " +
                $"{(int)response.StatusCode}.");
        }

        TokenResponse? tokenResponse;
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (stream.ConfigureAwait(false))
            {
                tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(
                    stream, SerializerOptions, cancellationToken);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "IDP token response for token key '{TokenKey}' could not be parsed as JSON.", tokenKey);
            throw new TokenAcquisitionException(
                $"IDP token response for token key '{tokenKey}' could not be parsed as JSON.", ex);
        }

        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken) || tokenResponse.ExpiresIn <= 0)
        {
            _logger.LogError(
                "IDP token response for token key '{TokenKey}' did not contain a usable access_token/expires_in.",
                tokenKey);
            throw new TokenAcquisitionException(
                $"IDP token response for token key '{tokenKey}' did not contain a usable " +
                "access_token/expires_in.");
        }

        var marginedSeconds = tokenResponse.ExpiresIn - ExpiryMarginSeconds;
        var expireTimeUtc = DateTime.UtcNow.AddSeconds(marginedSeconds > 0 ? marginedSeconds : 0);

        // Deliberately logs only the token key, the server-declared expires_in, and the computed
        // cache-until timestamp — never tokenResponse.AccessToken.
        _logger.LogInformation(
            "Acquired IDP access token for token key '{TokenKey}' (expires_in={ExpiresIn}s, cached until {ExpireTimeUtc:o}).",
            tokenKey,
            tokenResponse.ExpiresIn,
            expireTimeUtc);

        return new TokenModel
        {
            TokenResponse = tokenResponse,
            ExpireTimeUtc = expireTimeUtc,
        };
    }

    /// <summary>
    /// Disposes <see cref="_refreshLock"/>. Registered as a singleton, so the DI container calls
    /// this once at application shutdown (<c>ServiceProvider.Dispose</c>) — not per-request.
    /// </summary>
    public void Dispose()
    {
        _refreshLock.Dispose();
    }

    private static void ValidateDetail(string tokenKey, TokenManagerDetail detail)
    {
        List<string>? missing = null;

        if (string.IsNullOrWhiteSpace(detail.Server))
        {
            (missing ??= new List<string>()).Add(nameof(TokenManagerDetail.Server));
        }

        if (string.IsNullOrWhiteSpace(detail.ClientId))
        {
            (missing ??= new List<string>()).Add(nameof(TokenManagerDetail.ClientId));
        }

        if (string.IsNullOrWhiteSpace(detail.ClientSecret))
        {
            (missing ??= new List<string>()).Add(nameof(TokenManagerDetail.ClientSecret));
        }

        if (string.IsNullOrWhiteSpace(detail.GrantType))
        {
            (missing ??= new List<string>()).Add(nameof(TokenManagerDetail.GrantType));
        }

        if (missing is { Count: > 0 })
        {
            // Lists only property *names*, never detail.ClientSecret's value.
            throw new InvalidOperationException(
                $"IDP token manager '{tokenKey}' is missing required configuration: " +
                $"{string.Join(", ", missing)}. Configure these under \"Idp:{tokenKey}\" in " +
                "appsettings.json / User Secrets before requesting a token for this key.");
        }
    }
}
