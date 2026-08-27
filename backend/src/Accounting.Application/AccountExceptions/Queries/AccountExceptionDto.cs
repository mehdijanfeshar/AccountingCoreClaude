namespace Accounting.Application.AccountExceptions.Queries;

/// <summary>
/// Read-side projection of <c>TB_ACCOUNTEXCEPTION</c>. Used by both
/// <c>GetAccountExceptions</c> (list) and <c>GetAccountExceptionById</c> — the Domain entity
/// never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCoeId">
/// ACCOUNTCOE_ID column — FK to <c>TB_ACCOUNTCODE</c>. Name mirrors the real (typo'd) Oracle
/// column name — see <c>CreateAccountExceptionCommand.AccountCoeId</c> XML doc.
/// </param>
/// <param name="VahedTypeId">VAHEDTYPE_ID column — FK to <c>TB_VAHED_TYPE</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record AccountExceptionDto(
    Guid Id,
    Guid AccountCoeId,
    Guid VahedTypeId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
