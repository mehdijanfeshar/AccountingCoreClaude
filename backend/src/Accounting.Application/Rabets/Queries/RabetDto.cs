namespace Accounting.Application.Rabets.Queries;

/// <summary>
/// Read-side projection of <c>TB_RABET</c>. Used by both <c>GetRabets</c> (list) and
/// <c>GetRabetById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="RabetTypeId">RABETTYPE_ID column — link to <c>TB_RABET_TYPE</c>.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — link to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record RabetDto(
    Guid Id,
    Guid? RabetTypeId,
    Guid? AccountCodeId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
