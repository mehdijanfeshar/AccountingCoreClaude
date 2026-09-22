namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated caller asks for a specific row by <c>ID</c> that exists but
/// belongs to a different organizational unit. This closes the second half of IDOR risk #1: phase
/// 19 made list/search/create server-scoped, but <c>GetById</c>/<c>Update</c>/<c>Delete</c> took
/// the caller's <c>ID</c> at face value, so knowing an <c>ID</c> was enough to read, edit or
/// delete any unit's row.
///
/// <para>
/// <b>403, not 404 — a deliberate choice by the project owner.</b> Returning 404 would hide
/// whether the row exists at all, which is the stricter answer for a public API. This is an
/// internal organizational system, and the reference project (<c>BusinessUserAccess</c>) answers
/// with an explicit "this user has no access to unit X" error rather than pretending the record is
/// absent, because a finance officer who mistypes an ID needs to know the difference between
/// "wrong ID" and "not your unit". We follow the reference's intent but not its wording: the
/// response body says only that the caller's unit lacks access, and never names the owning unit —
/// the owning unit code appears in the exception message, which reaches the log, not the client.
/// </para>
///
/// <para>
/// <b>Rows with no unit are not access-controlled.</b> A blank or null <c>VAHEDCODE</c> marks a
/// globally-shared row (the reference project writes <c>VahedCode = ""</c> for
/// <c>Owner = Global</c> tafsili records), and those stay readable and writable by every unit.
/// Treating blank as "owned by nobody, therefore denied" would silently make every global record
/// unreachable.
/// </para>
///
/// This is an Application-level exception: it carries no Oracle/EF Core types and no table/column
/// names, so <c>Accounting.Api</c> maps it to <c>403 Forbidden</c> via
/// <c>GlobalExceptionHandler</c> without leaking Legacy schema detail.
/// </summary>
public sealed class UnitAccessDeniedException : Exception
{
    public UnitAccessDeniedException(string resourceName, Guid id, string? ownerVahedCode, string callerVahedCode)
        : base(
            $"{resourceName} with id '{id}' belongs to organizational unit '{ownerVahedCode}', " +
            $"but the authenticated caller's unit is '{callerVahedCode}'. Access denied.")
    {
    }
}
