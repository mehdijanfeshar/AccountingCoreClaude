namespace Accounting.Application.BillLogs.Queries;

/// <summary>
/// Read-side projection of <c>TB_BILL_LOG</c>. Used by both <c>GetBillLogs</c> (list) and
/// <c>GetBillLogById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="LogDesc">LOG_DESC column.</param>
/// <param name="LogDate">LOG_DATE column (a Persian date string, kept as-is).</param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record BillLogDto(
    Guid Id,
    string? LogDesc,
    string? LogDate,
    string? VahedCode,
    string? Year,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
