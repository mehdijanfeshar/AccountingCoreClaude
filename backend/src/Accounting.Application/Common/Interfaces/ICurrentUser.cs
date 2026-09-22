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
    /// The authenticated user's unit code (Tamin IDP <c>urn:tamin:jwt:claim:org</c> claim), if
    /// present. Wired into <c>VahedScopeBehavior</c> (phase 19) for server-side row scoping.
    /// </summary>
    string? VahedCode { get; }

    /// <summary>
    /// The organizational unit the client has asked to act as for this request, taken from the
    /// <c>X-Vahed-Code</c> request header, or <see langword="null"/> when the client asked for
    /// nothing.
    ///
    /// <para>
    /// ⚠️ <b>Untrusted.</b> Any client can set this header. It is never a scope on its own — it is
    /// only ever validated against the server-computed access set by
    /// <see cref="IUnitScopeResolver"/>, which is the only component that should read it.
    /// Everything else must use the resolver's answer, never this value.
    /// </para>
    /// </summary>
    string? RequestedVahedCode { get; }

    /// <summary>
    /// True when the current authenticated principal is a member of <paramref name="role"/>.
    /// Returns false (never throws) when there is no authenticated user.
    /// </summary>
    bool IsInRole(string role);
}
