namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Outbound OAuth2 <c>client_credentials</c> token acquirer, used when this API needs to call
/// another tamin service as a client. Infrastructure-internal by design: no Application use case
/// consumes a raw token acquirer today. When one exists, it should depend on a purpose-named
/// capability interface (e.g. <c>IPersonDirectoryClient</c>) defined in
/// <c>Accounting.Application/Common/Interfaces</c> that is implemented *using* this type — not on
/// <see cref="ITokenManager"/> directly. Do not add this interface to
/// <c>Accounting.Application/Common/Interfaces</c>.
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// Returns a valid access token for <paramref name="tokenKey"/>, reusing a cached token while
    /// it remains valid and transparently refreshing it otherwise. Never returns a stale or null
    /// token on failure — a failed refresh throws instead (see <see cref="TokenAcquisitionException"/>).
    /// </summary>
    /// <param name="tokenKey">
    /// Selects which <see cref="TokenManagerDetail"/> (registered via
    /// <c>AddTaminTokenManager</c>) to use, e.g. <c>"tamin"</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="tokenKey"/> is not configured, or its configuration is incomplete.
    /// </exception>
    /// <exception cref="TokenAcquisitionException">The IDP request failed or returned an unusable response.</exception>
    Task<string> GetAccessTokenAsync(string tokenKey, CancellationToken cancellationToken = default);
}
