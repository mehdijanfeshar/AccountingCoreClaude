namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Root configuration object passed to <c>AddTaminTokenManager</c>. Holds one
/// <see cref="TokenManagerDetail"/> per outbound IDP target, keyed by a caller-chosen string
/// (e.g. <c>"tamin"</c>) that callers later pass to <see cref="ITokenManager.GetAccessTokenAsync"/>.
/// </summary>
public sealed class TokenManagerConfiguration
{
    public Dictionary<string, TokenManagerDetail> TokenManagers { get; } = new(StringComparer.OrdinalIgnoreCase);
}
