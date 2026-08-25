namespace Accounting.Application.Vouchers.Queries;

/// <summary>
/// Flat read-side projection of <c>TB_VOUCHERSDETAIL</c>. Used by both
/// <c>GetVoucherDetails</c> (list) and <c>GetVoucherDetailById</c> — the Domain entity never
/// crosses the Application boundary, and no navigation property is ever included (no eager-load
/// of <c>ACCOUNT</c>/<c>RECEIP</c>/<c>VOUCHERSHEAD</c>/tafsili links).
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column — FK to <c>TB_VOUCHERSHEAD</c>.</param>
/// <param name="AccountId">ACCOUNT_ID column — optional FK to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="ReceiptId">RECEIP_ID column — optional FK to <c>TB_RECEIP</c>.</param>
/// <param name="CheckId">CHECK_ID column.</param>
/// <param name="LowLevelCodeId">LOWLEVELCODE_ID column.</param>
/// <param name="EtebarId">ETEBAR_ID column.</param>
/// <param name="Description">DESCRIPTION column — شرح ردیف.</param>
/// <param name="Radif">RADIF column — ردیف نمایش سطر.</param>
/// <param name="Debtor">DEBTOR column — مبلغ بدهکار.</param>
/// <param name="Creditor">CREDITOR column — مبلغ بستانکار.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="VahedCode">VAHEDCODE column — کد واحد.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record VoucherDetailDto(
    Guid Id,
    Guid? VoucherHeadId,
    Guid? AccountId,
    Guid? ReceiptId,
    Guid? CheckId,
    Guid? LowLevelCodeId,
    Guid? EtebarId,
    string? Description,
    int? Radif,
    decimal? Debtor,
    decimal? Creditor,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    string? VahedCode,
    string? Year,
    bool? IsDeleted);
