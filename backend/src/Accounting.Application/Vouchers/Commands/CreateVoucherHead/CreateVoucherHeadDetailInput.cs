namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// One initial <c>TB_VOUCHERSDETAIL</c> line to be created together with its parent
/// <c>TB_VOUCHERSHEAD</c> row by <see cref="CreateVoucherHeadCommand"/> (composite create,
/// option "الف" — 2026-08-20 project-owner decision recorded in
/// <c>docs/tamin-core-entity-reference.md</c> بخش ۵). Primitives only — see the XML doc on
/// <see cref="CreateVoucherHeadCommand"/> for why a nested primitive-only transport record does
/// not violate the "Command carries only primitives" rule.
///
/// Deliberately has NO <c>VoucherHeadId</c>: the head does not exist yet when this record is
/// built by the caller — <see cref="CreateVoucherHeadCommandHandler"/> generates the head's
/// <c>ID</c> first and wires every detail's <c>VOUCHERSHEAD_ID</c> to it. Also deliberately has
/// NO <c>VahedCode</c>/<c>Year</c>: both are derived from the head being created in the very
/// same call, never supplied independently per line. This structurally prevents a
/// head/line mismatch — the caller cannot even express "line belongs to a different voucher or
/// unit/year than its own head", the same way a phase-8 <c>UpdateXRequest</c> has no <c>Id</c>
/// to structurally prevent a route/body id mismatch.
/// </summary>
/// <param name="AccountId">ACCOUNT_ID column — optional FK to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="ReceiptId">RECEIP_ID column — optional FK to <c>TB_RECEIP</c>.</param>
/// <param name="CheckId">CHECK_ID column — optional FK (no enforced DB constraint; see <see cref="Accounting.Domain.Entity.TB_VOUCHERSDETAIL"/> mapping).</param>
/// <param name="LowLevelCodeId">LOWLEVELCODE_ID column — optional.</param>
/// <param name="EtebarId">ETEBAR_ID column — optional.</param>
/// <param name="Description">DESCRIPTION column — شرح ردیف (max 200 chars).</param>
/// <param name="Radif">RADIF column — ردیف نمایش سطر.</param>
/// <param name="Debtor">DEBTOR column — مبلغ بدهکار.</param>
/// <param name="Creditor">CREDITOR column — مبلغ بستانکار.</param>
public sealed record CreateVoucherHeadDetailInput(
    Guid? AccountId,
    Guid? ReceiptId,
    Guid? CheckId,
    Guid? LowLevelCodeId,
    Guid? EtebarId,
    string? Description,
    int? Radif,
    decimal? Debtor,
    decimal? Creditor);
