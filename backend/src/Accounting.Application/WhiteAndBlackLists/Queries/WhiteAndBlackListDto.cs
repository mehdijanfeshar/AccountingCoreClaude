using Accounting.Domain.ValueObjects;

namespace Accounting.Application.WhiteAndBlackLists.Queries;

/// <summary>
/// Read-side projection of <c>TB_WHITEANDBLACKLIST</c>. Used by both
/// <c>GetWhiteAndBlackLists</c> (list) and <c>GetWhiteAndBlackListById</c> — the Domain entity
/// never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — link to <c>TB_ACCOUNTCODE</c> (non-nullable in Legacy).</param>
/// <param name="VahedTypeId">VAHEDTYPE_ID column — optional link to <c>TB_VAHED_TYPE</c>.</param>
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
/// <param name="State">
/// STATE column (nullable <see cref="Accounting.Domain.ValueObjects.WhiteBlackListState"/>).
/// Resolved per <c>docs/centralaccount-business-reference.md</c> §24-1 (phase 27 batch 3).
/// </param>
public sealed record WhiteAndBlackListDto(
    Guid Id,
    Guid AccountCodeId,
    Guid? VahedTypeId,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string AddUserId,
    string? ChangeUserId,
    bool? IsDeleted,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate,
    WhiteBlackListState? State);
