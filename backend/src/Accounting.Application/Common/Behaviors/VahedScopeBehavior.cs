using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that closes IDOR risk #1 (CLAUDE.md "ریسک‌های باز") by forcing
/// <c>VAHEDCODE</c> to be server-assigned for every request that opts in via
/// <see cref="IVahedScoped"/>. Runs for <b>every</b> request in the assembly (it is a generic
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/>, registered once in
/// <c>DependencyInjection.cs</c>) — no per-handler wiring is possible, which is the entire point:
/// the CentralAccount reference project attempted the equivalent per-handler and only 12 of 372
/// handlers actually did it (CLAUDE.md phase 17).
///
/// Requests that do not implement <see cref="IVahedScoped"/> pass through completely untouched —
/// this behavior only inspects/mutates requests that opt in.
///
/// ⚠️ <b>Critical, non-obvious constraint choice — copy this exactly, do not "clean up" the type
/// parameters.</b> This class deliberately does <b>not</b> declare
/// <c>where TRequest : IRequest&lt;TResponse&gt;</c> (the constraint <c>ValidationBehavior</c>
/// uses). Verified empirically against this project's actual MediatR 14.2.0: a void command
/// (<c>: IRequest</c>, no generic response — e.g. every <c>Update</c>/<c>Delete</c> command in
/// this project) does <b>not</b> implement <c>IRequest&lt;Unit&gt;</c> in this MediatR version
/// (unlike older MediatR where <c>IRequest : IRequest&lt;Unit&gt;</c> held). The .NET DI
/// container's open-generic resolution silently — no exception, no log — skips any open-generic
/// <c>IPipelineBehavior&lt;,&gt;</c> registration whose implementation type's generic constraints
/// aren't satisfiable for a given closed request/response pair. Concretely: with the
/// <c>IRequest&lt;TResponse&gt;</c> constraint in place, this behavior (and, it turns out,
/// <c>ValidationBehavior</c> — a pre-existing, separate bug now on record, out of this class's
/// scope to fix) never runs for <c>UpdateWorkShopCommand</c>/<c>DeleteWorkShopCommand</c>-shaped
/// requests: no exception, no validation, no VahedCode enforcement — a textbook silent bypass of
/// exactly the kind this mechanism exists to prevent. Dropping the constraint (leaving only the
/// <c>notnull</c> that <see cref="IPipelineBehavior{TRequest,TResponse}"/> itself requires) makes
/// MediatR's internal void-request dispatch — which genuinely does invoke
/// <c>IPipelineBehavior&lt;TRequest, Unit&gt;</c>, just never via a constrained open-generic
/// registration — resolve this behavior correctly for both response-bearing and void requests.
/// This was confirmed with a standalone MediatR 14.2.0 + DI probe before this file was written;
/// see the task's final report for the reproduction. <b>Any of the 4 parallel batches copying
/// this behavior for their own entities must not "fix" this back to
/// <c>IRequest&lt;TResponse&gt;</c></b> — doing so reopens IDOR risk #1 specifically on every
/// Update command, silently.
///
/// For requests that do implement <see cref="IVahedScoped"/>, the contract is unconditional:
/// <list type="number">
/// <item><description>Read <see cref="ICurrentUser.VahedCode"/>.</description></item>
/// <item><description>If it is null, empty, or whitespace-only →
/// <see cref="MissingVahedScopeException"/> (fail-loud; see that type's XML doc).</description></item>
/// <item><description>If it is longer than <see cref="MaxVahedCodeLength"/> →
/// <see cref="MissingVahedScopeException"/> as well. Never truncate.</description></item>
/// <item><description>Otherwise, overwrite <c>request.VahedCode</c> with it — <b>regardless of
/// what the caller/client already put there</b>. Whatever a request body deserialized into
/// <c>VahedCode</c> is discarded without inspection; there is no "only overwrite if empty"
/// escape hatch, because that would just move the IDOR hole from "trust the client value" to
/// "trust the client value when the client is clever enough to leave the field blank".</description></item>
/// </list>
/// </summary>
public sealed class VahedScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Every <c>VAHEDCODE</c> mapping in <c>LegacyDbContext</c> (45 of them, with zero
    /// exceptions) is declared <c>HasMaxLength(4)</c>. A claim value longer than this can never
    /// be written to Legacy, so it is rejected here rather than surfacing as a downstream Oracle
    /// error or, worse, a silently truncated value.
    /// </summary>
    public const int MaxVahedCodeLength = 4;

    private readonly ICurrentUser _currentUser;

    public VahedScopeBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IVahedScoped scoped)
        {
            var vahedCode = _currentUser.VahedCode;

            if (string.IsNullOrWhiteSpace(vahedCode))
            {
                throw new MissingVahedScopeException(typeof(TRequest).Name);
            }

            if (vahedCode.Length > MaxVahedCodeLength)
            {
                throw new MissingVahedScopeException(typeof(TRequest).Name);
            }

            // Unconditional: whatever the caller supplied is discarded, not merely defaulted.
            scoped.VahedCode = vahedCode;
        }

        return next(cancellationToken);
    }
}
