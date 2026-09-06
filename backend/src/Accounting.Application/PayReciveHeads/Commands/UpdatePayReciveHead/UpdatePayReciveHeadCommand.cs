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
/// </summary>
/// <param name="Id">The <c>TB_PAYRECIVHEAD.ID</c> to update (bound from the route).</param>
/// <param name="PayReciveCode">PAYRECIVCODE column (required, max 5 chars). ⚠️ NOT unique — no UNIQUE constraint exists, and no duplicate guard is implemented.</param>
/// <param name="PayReciveDate">PAYRECIVDATE column (required, max 8 chars — Legacy string-encoded date).</param>
/// <param name="PayReciveDescription">PAYRECIVDESCRIPTION column (required, max 250 chars).</param>
/// <param name="PayReciveType">PAYRECIVTYPE column (optional <see cref="bool"/>) — ⚠️ CONFIRMED enum-should-be, real values 1/2/3.</param>
/// <param name="VahedCode">VAHEDCODE column (required, max 4 chars).</param>
/// <param name="Year">YEAR column (required, max 4 chars, fixed-length).</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column (optional, <c>FK_PAYRECIV_VOCHERHEAD</c> → 400 on violation).</param>
public sealed record UpdatePayReciveHeadCommand(
    Guid Id,
    string PayReciveCode,
    string PayReciveDate,
    string PayReciveDescription,
    bool? PayReciveType,
    string VahedCode,
    string Year,
    Guid? VoucherHeadId) : IRequest;
