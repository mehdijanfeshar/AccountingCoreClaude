using System.Text.Json.Serialization;

namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Cache entry held per <c>tokenKey</c> inside <see cref="TokenManager"/>. Deliberately internal:
/// nothing outside the Idp folder needs the raw <see cref="TokenResponse"/> or expiry bookkeeping,
/// only the resulting access token string returned by <see cref="ITokenManager.GetAccessTokenAsync"/>.
/// </summary>
internal sealed class TokenModel
{
    public required TokenResponse TokenResponse { get; init; }

    /// <summary>
    /// UTC instant after which this token must be treated as expired and refreshed. Computed as
    /// <c>issued-at (UTC) + expires_in - 30s</c> safety margin — see
    /// <see cref="TokenManager.ExpiryMarginSeconds"/>. Always UTC; never <see cref="DateTime.Now"/>.
    /// </summary>
    public required DateTime ExpireTimeUtc { get; init; }
}

/// <summary>
/// Shape of the token endpoint's JSON response. Only the fields this codebase actually reads are
/// mapped; unknown fields are ignored by <c>System.Text.Json</c> by default.
/// </summary>
internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}
