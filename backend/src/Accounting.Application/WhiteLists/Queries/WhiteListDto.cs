namespace Accounting.Application.WhiteLists.Queries;

/// <summary>
/// Read-side projection of <c>TB_WHITELIST</c>. Used by both <c>GetWhiteLists</c> (list) and
/// <c>GetWhiteListById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — link to <c>TB_ACCOUNTCODE</c> (non-nullable in Legacy).</param>
/// <param name="VahedTypeId">VAHEDTYPE_ID column — optional link to <c>TB_VAHED_TYPE</c>.</param>
/// <param name="VahedInfoId">VAHEDINFO_ID column — optional link to <c>TB_VAHED_INFO</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp (non-nullable in Legacy).</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier (non-nullable in Legacy).</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
/// <param name="FromAuthorizedDate">FROMAUTHORIZEDDATE column.</param>
/// <param name="ToAuthorizedDate">TOAUTHORIZEDDATE column.</param>
/// <param name="FromLimitationDate">FROMLIMITATIONDATE column.</param>
/// <param name="ToLimitationDate">TOLIMITATIONDATE column.</param>
public sealed record WhiteListDto(
    Guid Id,
    Guid AccountCodeId,
    Guid? VahedTypeId,
    Guid? VahedInfoId,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string AddUserId,
    string? ChangeUserId,
    bool? IsDeleted,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate);
