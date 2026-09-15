using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.UpdateBillLog;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_BILL_LOG</c> row (PUT semantics, not
/// PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_BILL_LOG.ID</c> to update (bound from the route, never the body).</param>
/// <param name="LogDesc">LOG_DESC column (max 1000 chars, optional free-text description).</param>
/// <param name="LogDate">LOG_DATE column (max 8 chars — a Persian date string).</param>
/// <param name="Year">YEAR column (max 4 chars, fixed-length, required fiscal year).</param>
public sealed record UpdateBillLogCommand(
    Guid Id,
    string? LogDesc,
    string? LogDate,
    string Year) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required organizational unit code). Never bound from the
    /// request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and
    /// the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>UpdateBillLogCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
