using Accounting.Application.Vouchers.Commands.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_VOUCHERSDETAIL</c> row (PUT
/// semantics, not PATCH) — mirrors <c>UpdateVoucherHeadCommand</c>'s replace-vs-patch rationale
/// (almost every field here is nullable, so partial-update cannot distinguish "omitted" from
/// "explicitly null" without disproportionate machinery).
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c>, <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteVoucherDetailCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
///
/// <b>Also deliberately excludes <c>VoucherHeadId</c>/<c>VOUCHERSHEAD_ID</c> — open decision,
/// NOT guessed:</b> the parent voucher is treated as immutable alongside <c>ID</c>. Reparenting
/// a detail line to a different voucher head is conceptually a MOVE, not an EDIT of the line's
/// own fields, and no business decision authorizes that operation; if it is ever needed it
/// should be its own explicit use case (e.g. <c>MoveVoucherDetailCommand</c>), not smuggled into
/// a field-level replace here.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSDETAIL.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountId">ACCOUNT_ID column — optional FK to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="ReceiptId">RECEIP_ID column — optional FK to <c>TB_RECEIP</c>.</param>
/// <param name="CheckId">CHECK_ID column.</param>
/// <param name="LowLevelCodeId">LOWLEVELCODE_ID column.</param>
/// <param name="EtebarId">ETEBAR_ID column.</param>
/// <param name="Description">DESCRIPTION column — شرح ردیف (max 200 chars).</param>
/// <param name="Radif">RADIF column — ردیف نمایش سطر.</param>
/// <param name="Debtor">DEBTOR column — مبلغ بدهکار.</param>
/// <param name="Creditor">CREDITOR column — مبلغ بستانکار.</param>
/// <param name="VahedCode">VAHEDCODE column — کد واحد (max 4 chars).</param>
/// <param name="Year">YEAR column (max 4 chars).</param>
/// <param name="TafsiliLinks">
/// The complete desired set of تفصیلی assignments (<c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> rows) for
/// this line. <b>Full replacement, not a patch and not append-only</b>: links present here but not
/// in the database are inserted, links in the database but absent here are soft-deleted, and links
/// in both are left byte-identically untouched (their <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// NOT re-stamped, since nothing about them changed). This matches the replace-not-patch semantics
/// the rest of this command already has, and it is the only shape that lets a caller ever REMOVE a
/// تفصیلی from a line — an append-only rule would make removal permanently impossible through the
/// API, since the table has no CRUD of its own by design.
///
/// <b><see langword="null"/> means "leave the existing links exactly as they are"; an empty list
/// means "this line should end up with no links" (i.e. soft-delete all of them).</b> These two are
/// deliberately NOT collapsed, even though every other field on this command treats an omitted
/// JSON property as an explicit null. The reason is specific and not a matter of taste: the read
/// side does not expose تفصیلی links at all (<c>VoucherDetailDto</c> has no links property), so a
/// client physically cannot perform a read-modify-write round trip on them. Had omission meant
/// "clear", every existing well-behaved client — none of which know this field exists — would
/// silently destroy تفصیلی data on the next unrelated line edit. Requiring the caller to opt in by
/// sending the property makes destruction explicit, and keeps this change non-breaking for the
/// callers written before the field existed.
///
/// Duplicate <c>(TafsiliId, LevelId)</c> pairs within the supplied list are collapsed to a single
/// row, same as on <c>CreateVoucherDetailCommand</c>. Re-requesting a pair that exists only as a
/// previously soft-deleted row inserts a NEW row rather than resurrecting the old one — the
/// soft-deleted row stays deleted, preserving its original audit trail.
/// </param>
public sealed record UpdateVoucherDetailCommand(
    Guid Id,
    Guid? AccountId,
    Guid? ReceiptId,
    Guid? CheckId,
    Guid? LowLevelCodeId,
    Guid? EtebarId,
    string? Description,
    int? Radif,
    decimal? Debtor,
    decimal? Creditor,
    string? VahedCode,
    string? Year,
    IReadOnlyList<VoucherDetailTafsiliLinkInput>? TafsiliLinks = null) : IRequest;
