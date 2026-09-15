namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <see cref="Accounting.Application.Common.Behaviors.VahedScopeBehavior{TRequest,TResponse}"/>
/// when a request implementing <see cref="Accounting.Application.Common.Security.IVahedScoped"/>
/// cannot be assigned a valid <c>VAHEDCODE</c> — either because
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser.VahedCode"/> is null/empty/
/// whitespace, or because it exceeds the 4-character <c>VAHEDCODE</c> column width enforced
/// everywhere in <c>LegacyDbContext</c> (<c>HasMaxLength(4)</c>, all 45 mappings).
///
/// This is <b>deliberate fail-loud behaviour</b>, not a defect to be silently worked around: the
/// authenticated caller's IDP token either carries a usable unit-scope claim or it does not, and
/// letting the request through with a missing/invalid/truncated <c>VAHEDCODE</c> would silently
/// defeat the entire IDOR mitigation this behavior exists for (CLAUDE.md risk #1). Truncating the
/// value instead of throwing is explicitly rejected for the same reason
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser.UserId"/> refuses to truncate
/// an over-length identity.
///
/// This is an Application-level exception: it carries no Oracle/EF Core types and no table/column
/// names, so <c>Accounting.Api</c> can map it to <c>403 Forbidden</c> via
/// <c>GlobalExceptionHandler</c> without leaking any Legacy schema detail in the response body.
/// </summary>
public sealed class MissingVahedScopeException : Exception
{
    public MissingVahedScopeException(string requestTypeName)
        : base(
            $"Cannot dispatch '{requestTypeName}': the authenticated caller has no usable " +
            "VahedCode (unit-scope) claim. This is a deliberate fail-loud guard, not a bug — " +
            "requests are never silently allowed through with a missing, empty, or over-length " +
            "VahedCode.")
    {
    }
}
