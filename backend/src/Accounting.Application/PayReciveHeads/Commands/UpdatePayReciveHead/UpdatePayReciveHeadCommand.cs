using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_PAYRECIVHEAD</c> row (PUT semantics,
/// not PATCH — see CLAUDE.md phase 8 for why partial updates are not modelled on Legacy tables).
/// Every field the caller omits becomes <see langword="null"/> or fails validation; there is no
/// "leave unchanged" marker.
///
/// <c>Id</c> is bound from the route by the controller, never from the request body — the
/// controller's <c>UpdatePayReciveHeadRequest</c> record has no <c>Id</c> member at all, so a
/// route/body id mismatch is structurally impossible.
///
/// <b>Columns deliberately absent from this command (immutable after creation):</b>
/// <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> (creation audit trail — rewriting them would
/// destroy it) and <c>ISDELETED</c> (exposing it here would turn Update into a back-door
/// delete/undelete path; deletion goes through <c>DeletePayReciveHeadCommand</c> only).
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are absent too, but for the opposite reason: the
/// handler always stamps them server-side from <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/>.
///
/// ⚠️ HEAD ONLY, and ⚠️⚠️ <c>PayReciveType</c> is a CONFIRMED <c>bool?</c>-should-be-enum column
/// (real values 1/2/3) — see <c>CreatePayReciveHeadCommand</c> XML doc for both write-ups.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update is explicitly out of scope for this pass, by
/// project-owner decision. The IDOR risk on direct-by-id access therefore remains open for
/// Update; only the "what unit does this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_PAYRECIVHEAD.ID</c> to update (bound from the route).</param>
/// <param name="PayReciveCode">PAYRECIVCODE column (required, max 5 chars). ⚠️ NOT unique — no UNIQUE constraint exists, and no duplicate guard is implemented.</param>
/// <param name="PayReciveDate">PAYRECIVDATE column (required, max 8 chars — Legacy string-encoded date).</param>
/// <param name="PayReciveDescription">PAYRECIVDESCRIPTION column (required, max 250 chars).</param>
/// <param name="PayReciveType">PAYRECIVTYPE column (optional <see cref="bool"/>) — ⚠️ CONFIRMED enum-should-be, real values 1/2/3.</param>
/// <param name="Year">YEAR column (required, max 4 chars, fixed-length).</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column (optional, <c>FK_PAYRECIV_VOCHERHEAD</c> → 400 on violation).</param>
public sealed record UpdatePayReciveHeadCommand(
    Guid Id,
    string PayReciveCode,
    string PayReciveDate,
    string PayReciveDescription,
    bool? PayReciveType,
    string Year,
    Guid? VoucherHeadId) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (required, max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdatePayReciveHeadCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
