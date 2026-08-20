namespace Accounting.Infrastructure.Idp;

/// <summary>
/// Thrown by <see cref="TokenManager"/> when an outbound token refresh fails — network failure,
/// non-success HTTP status, or an unparsable/incomplete response. This is the propagation
/// mechanism for defect fix #1 (the ported pattern used to swallow this via
/// <c>catch (HttpRequestException) { Console.WriteLine(...); }</c> and silently return whatever
/// stale/null token was cached). The message never contains the configured client secret or any
/// access token value — only the token key, status code, and/or a short reason.
/// </summary>
public sealed class TokenAcquisitionException : Exception
{
    public TokenAcquisitionException(string message)
        : base(message)
    {
    }

    public TokenAcquisitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
