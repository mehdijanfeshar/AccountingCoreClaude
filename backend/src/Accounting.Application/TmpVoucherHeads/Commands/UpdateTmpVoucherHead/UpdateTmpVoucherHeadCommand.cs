using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_TMP_VOUCHERHEAD</c> row (PUT
/// semantics, not PATCH — see CLAUDE.md phase 8 for why partial updates are not modelled on
/// Legacy tables). Because every column reachable through the positional parameters here is
/// nullable, any field the caller omits is written as <see langword="null"/>; there is no
/// "leave unchanged" marker.
///
/// <c>Id</c> is bound from the route by the controller, never from the request body — the
/// controller's <c>UpdateTmpVoucherHeadRequest</c> record has no <c>Id</c> member at all, so a
/// route/body id mismatch is structurally impossible.
///
/// <b>Columns deliberately absent from this command (immutable after creation):</b>
/// <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> (creation audit trail — rewriting them would
/// destroy it) and <c>ISDELETED</c> (exposing it here would turn Update into a back-door
/// delete/undelete path; deletion goes through <c>DeleteTmpVoucherHeadCommand</c> only).
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are absent too, but for the opposite reason: the
/// handler always stamps them server-side from <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/>.
///
/// ⚠️ HEAD ONLY, and ⚠️⚠️ <c>SourceId</c> has NO FK — see
/// <c>CreateTmpVoucherHeadCommand</c> XML doc for both write-ups.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_TMP_VOUCHERHEAD.ID</c> to update (bound from the route).</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column (optional, <c>FK_TMP_VOCHERHEAD</c> → 400 on violation).</param>
/// <param name="DateDoc">DATE_DOC column (optional, max 8 chars — Legacy string-encoded date).</param>
/// <param name="HeadDesc">HEAD_DESC column (optional, max 250 chars).</param>
/// <param name="Year">YEAR column (optional, max 4 chars, fixed-length).</param>
/// <param name="SysType">SYS_TYPE column (optional, max 1 char — source-system discriminator).</param>
/// <param name="SourceId">SOURCEID column (optional) — ⚠️ NO FK at all; invalid values are written silently.</param>
public sealed record UpdateTmpVoucherHeadCommand(
    Guid Id,
    Guid? VoucherHeadId,
    string? DateDoc,
    string? HeadDesc,
    string? Year,
    string? SysType,
    Guid? SourceId) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (<c>VAHEDCODE</c> column — nullable at the Legacy schema level,
    /// but always populated with a real value here). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdateTmpVoucherHeadCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
