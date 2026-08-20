namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Server-side source of truth for "who is making this request", populated by the
/// authentication middleware (JWT bearer — see <c>Program.cs</c> in <c>Accounting.Api</c>).
/// Handlers depend on this instead of trusting any client-supplied "created by" field, so that
/// Legacy audit columns such as <c>ADDUSERID</c> cannot be forged by the caller.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// True when the current request carries a validated, authenticated principal.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The authenticated user's identifier, sourced from the <c>NameIdentifier</c> claim.
    /// Throws <see cref="InvalidOperationException"/> if read while unauthenticated, or if the
    /// claim value is longer than the 10-character Legacy <c>ADDUSERID</c>/<c>CHANGEUSERID</c>
    /// audit columns — this must fail loudly rather than silently truncate an identity.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// The authenticated user's unit code (<c>vahed_code</c> claim), if present. Exposed for
    /// future authorization/filtering use cases — not currently wired into any query filter.
    /// </summary>
    string? VahedCode { get; }

    /// <summary>
    /// True when the current authenticated principal is a member of <paramref name="role"/>.
    /// Returns false (never throws) when there is no authenticated user.
    /// </summary>
    bool IsInRole(string role);
}
