namespace Accounting.Application.Receipts.Queries;

/// <summary>
/// Read-side projection of <c>TB_RECEIP</c>. Used by both <c>GetReceipts</c> (list) and
/// <c>GetReceiptById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="ReceiptKind">RECEIPT_KIND column — see the unverified-enum note on <c>CreateReceiptCommand</c>.</param>
/// <param name="ReceiptDate">RECEIPT_DATE column.</param>
/// <param name="ReceiptNo">RECEIPT_NO column.</param>
/// <param name="DateRsid">DATE_RSID column.</param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag, exposed as-is.</param>
public sealed record ReceiptDto(
    Guid Id,
    bool ReceiptKind,
    string ReceiptDate,
    string ReceiptNo,
    string? DateRsid,
    string VahedCode,
    string Year,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
