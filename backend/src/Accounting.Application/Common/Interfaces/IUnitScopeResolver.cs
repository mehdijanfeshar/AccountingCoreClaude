namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Decides the <b>effective</b> organizational unit for the current request — the single place
/// that answers "whose rows is this request allowed to touch".
///
/// <para>
/// <b>What changed in phase 37-B, and why.</b> Phases 19/32/33 answered that question with the
/// token's org claim and nothing else (exact equality). That was correct while a user could only
/// ever act as their own unit. The «تغییر واحد» feature means a user may deliberately act as a
/// unit inside their subtree, so the answer becomes a decision rather than a lookup:
/// <list type="number">
/// <item><description>No unit requested → the caller's own unit. Unchanged behaviour, and the
/// default for every existing client.</description></item>
/// <item><description>The caller's own unit requested → same thing, no tree walk needed.</description></item>
/// <item><description>Another unit requested → allowed only if
/// <see cref="IUnitAccessReadRepository"/> says the caller may act as it; otherwise
/// <c>UnitAccessDeniedException</c> (403).</description></item>
/// </list>
/// </para>
///
/// <para>
/// ⚠️ <b>The requested unit is untrusted input.</b> It arrives on a request header that any client
/// can set, exactly like a request body field. It is never used as-is; it is only ever
/// <i>validated against</i> the server-computed access set. The property that matters is that
/// this type is the only thing that turns untrusted input into an effective scope, so there is
/// one place to audit rather than 60.
/// </para>
/// </summary>
public interface IUnitScopeResolver
{
    /// <summary>
    /// Returns the unit code this request must operate as.
    ///
    /// Throws <c>MissingVahedScopeException</c> when the token carries no usable unit, and
    /// <c>UnitAccessDeniedException</c> when a unit was requested that the caller may not act as.
    /// Never returns a code the caller is not entitled to.
    /// </summary>
    Task<string> ResolveEffectiveVahedCodeAsync(CancellationToken cancellationToken = default);
}
