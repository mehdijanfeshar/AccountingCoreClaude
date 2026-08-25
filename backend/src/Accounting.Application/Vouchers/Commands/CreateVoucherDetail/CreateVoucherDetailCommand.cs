using Accounting.Application.Vouchers.Commands.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherDetail;

/// <summary>
/// Creates a new <c>TB_VOUCHERSDETAIL</c> row (Legacy voucher detail line) as a standalone
/// write against an ALREADY-EXISTING voucher head — e.g. adding a line to a voucher created
/// earlier. For creating a head together with its opening lines in one call, see
/// <see cref="Accounting.Application.Vouchers.Commands.CreateVoucherHead.CreateVoucherHeadCommand.InitialDetails"/>
/// instead. Carries primitive fields only — the handler is responsible for constructing the
/// Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// <b>Open decision, NOT guessed:</b> unlike the composite-create path (where
/// <c>VahedCode</c>/<c>Year</c> are always derived from the head being created in the same
/// call), here they are caller-supplied and NOT cross-checked against the parent head's own
/// <c>VAHEDCODE</c>/<c>YEAR</c>. Whether a standalone create should instead derive/validate them
/// from the parent head (making this asymmetry with the composite path go away) is an explicit
/// open decision left to <c>team-lead</c>/the project owner — it was deliberately not resolved
/// unilaterally here.
/// </summary>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column — FK to the existing <c>TB_VOUCHERSHEAD</c> this line belongs to.</param>
/// <param name="AccountId">ACCOUNT_ID column — optional FK to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="ReceiptId">RECEIP_ID column — optional FK to <c>TB_RECEIP</c>.</param>
/// <param name="CheckId">CHECK_ID column.</param>
/// <param name="LowLevelCodeId">LOWLEVELCODE_ID column.</param>
/// <param name="EtebarId">ETEBAR_ID column.</param>
/// <param name="Description">DESCRIPTION column — شرح ردیف (max 200 chars).</param>
/// <param name="Radif">RADIF column — ردیف نمایش سطر.</param>
/// <param name="Debtor">DEBTOR column — مبلغ بدهکار.</param>
/// <param name="Creditor">CREDITOR column — مبلغ بستانکار.</param>
/// <param name="VahedCode">VAHEDCODE column — کد واحد (max 4 chars). See the asymmetry note above.</param>
/// <param name="Year">YEAR column (max 4 chars). See the asymmetry note above.</param>
/// <param name="TafsiliLinks">
/// Optional تفصیلی assignments (<c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> rows) to create together
/// with this detail line, in the SAME
/// <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/> call — so a
/// line can never be observed without the تفصیلی it was created with. <see langword="null"/> or
/// an empty list behaves byte-identically to a plain line-only create, keeping this strictly
/// non-breaking for every existing caller (the parameter is last and defaulted, mirroring
/// <c>CreateVoucherHeadCommand.InitialDetails</c>).
///
/// This is the write half of a table that until now could only be <i>deleted</i> (by the phase-9/10
/// cascades) — see <see cref="VoucherDetailTafsiliLinkInput"/> for why nesting it here is NOT
/// independent CRUD for an embedded table.
///
/// Duplicate <c>(TafsiliId, LevelId)</c> pairs within the supplied list are collapsed to a single
/// row by the handler rather than rejected: the same تفصیلی assigned twice to one line is one
/// assignment, and the Legacy table has no UNIQUE constraint to lean on
/// (<c>PK_VOUCHERDETAILLINKTAF</c> covers only <c>ID</c>), so silently writing two identical rows
/// would leave unremovable duplicate data.
/// </param>
public sealed record CreateVoucherDetailCommand(
    Guid VoucherHeadId,
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
    IReadOnlyList<VoucherDetailTafsiliLinkInput>? TafsiliLinks = null) : IRequest<Guid>;
