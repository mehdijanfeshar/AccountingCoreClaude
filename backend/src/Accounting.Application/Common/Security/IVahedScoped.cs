using Accounting.Application.Common.Interfaces;

namespace Accounting.Application.Common.Security;

/// <summary>
/// Marker interface implemented by any MediatR request whose <c>VAHEDCODE</c> (organizational
/// unit) value must be supplied by the server, never by the caller. Detected by
/// <see cref="Accounting.Application.Common.Behaviors.VahedScopeBehavior{TRequest,TResponse}"/>,
/// which unconditionally overwrites <see cref="VahedCode"/> with
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser.VahedCode"/> before the
/// request reaches its handler. This is the single, project-wide mechanism that closes IDOR risk
/// #1 recorded in <c>CLAUDE.md</c> — see that file's "ریسک‌های باز" table.
///
/// <b>Why <see cref="VahedCode"/> is a mutable property, not an <c>init</c>-only one or a
/// positional record parameter.</b> MediatR's (this project is on 14.2.0) pipeline signature is
/// <c>RequestHandlerDelegate&lt;TResponse&gt; next(CancellationToken)</c> — the behavior receives
/// the already-constructed <c>TRequest</c> instance and has no way to swap in a replacement
/// (a C# 9 <c>with</c>-expression would produce a *new* instance that <c>next</c> never sees,
/// since <c>next</c> closes over the original). A settable property is therefore the only way for
/// the pipeline to mutate the value that the handler will actually observe. This is deliberately
/// the same shape as the existing <c>ADDUSERID</c> pattern (<see cref="ICurrentUser"/> +
/// handler-side assignment), just moved one layer earlier — into the pipeline instead of into
/// each handler — so that forgetting to wire it per-handler (the exact mistake the CentralAccount
/// reference project made, per CLAUDE.md phase 17: only 12 of 372 handlers actually enforced it)
/// is no longer possible.
/// </summary>
public interface IVahedScoped
{
    /// <summary>
    /// The <c>VAHEDCODE</c> value that will be applied to this request. Any value the caller
    /// sets here before the request is dispatched (e.g. via a deserialized request body) is
    /// discarded — <see cref="Accounting.Application.Common.Behaviors.VahedScopeBehavior{TRequest,TResponse}"/>
    /// unconditionally replaces it with the authenticated caller's own unit code.
    /// </summary>
    string VahedCode { get; set; }
}

/// <summary>
/// Marks a read-side query (list/search) whose results must be scoped to the caller's
/// organizational unit. See <see cref="IVahedScoped"/> for the full mechanism.
/// </summary>
public interface IVahedScopedQuery : IVahedScoped
{
}

/// <summary>
/// Marks a write-side command (Create, or the subset of an Update that touches
/// <c>VAHEDCODE</c>) whose <c>VAHEDCODE</c> must be server-assigned. See
/// <see cref="IVahedScoped"/> for the full mechanism.
/// </summary>
public interface IVahedScopedCommand : IVahedScoped
{
}
