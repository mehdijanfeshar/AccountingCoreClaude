namespace Accounting.Application.WorkShops.Queries;

/// <summary>
/// Read-side projection of <c>TB_WORKSHOP</c>. Used by both <c>GetWorkShops</c> (list) and
/// <c>GetWorkShopById</c> — the Domain entity never crosses the Application boundary.
///
/// ⚠️ Deliberately excludes <c>CheckFile</c> (the BLOB column): list/detail projections are not
/// the place to stream binary content — a dedicated download endpoint would be the appropriate
/// place for that, and none exists yet (out of scope for this batch).
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — required link to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="BranchId">BRANCH_ID column — optional link to <c>TB_VAHED_INFO</c>.</param>
/// <param name="WorkShopName">WORKSHOPNAME column.</param>
/// <param name="WorkShopCode">WORKSHOPCODE column — part of <c>UK_WORKSHOP</c>.</param>
/// <param name="VahedCode">VAHEDCODE column — part of <c>UK_WORKSHOP</c>.</param>
/// <param name="IsActive">ISACTIVE column — non-nullable flag, part of <c>UK_WORKSHOP</c>; see <c>CreateWorkShopCommand</c> XML doc for the unverified-enum note.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record WorkShopDto(
    Guid Id,
    Guid AccountCodeId,
    Guid? BranchId,
    string WorkShopName,
    string WorkShopCode,
    string VahedCode,
    bool IsActive,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
