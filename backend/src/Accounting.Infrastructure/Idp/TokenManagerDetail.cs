namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Configuration for a single outbound OAuth2 <c>client_credentials</c> token-acquisition
/// target, keyed by a caller-chosen string (e.g. <c>"tamin"</c>) inside
/// <see cref="TokenManagerConfiguration.TokenManagers"/>. Bound from the <c>Idp:&lt;key&gt;</c>
/// configuration section (see <c>appsettings.json</c> / User Secrets) by
/// <c>Accounting.Infrastructure.DependencyInjection</c>.
/// </summary>
public sealed class TokenManagerDetail
{
    /// <summary>Full token endpoint URL of the IDP (e.g. <c>https://idp.example/connect/token</c>).</summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>Per-request HTTP timeout. Defaults to 30 seconds, matching the ported pattern.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public string ClientId { get; set; } = string.Empty;

    public string? Audience { get; set; }

    /// <summary>OAuth2 grant type. Always <c>client_credentials</c> for this outbound flow.</summary>
    public string GrantType { get; set; } = "client_credentials";

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Configured (bound from <c>Idp:&lt;key&gt;:External_Resources</c>) but intentionally NOT
    /// sent as part of the token request today — this mirrors the ported behaviour exactly.
    /// Do not start sending a <c>resource</c> parameter built from this collection until the
    /// project owner confirms whether the target IDP actually requires one; see
    /// <see cref="TokenManager"/> for where the request body is built.
    /// </summary>
    public ICollection<string> Resources { get; set; } = new List<string>();
}
