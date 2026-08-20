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
    string? Year) : IRequest;
