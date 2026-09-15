namespace Accounting.Application.IdentityGroups.Queries;

/// <summary>
/// Read-side projection of <c>TB_IDENTITYGROUP</c>. Used by both <c>GetIdentityGroups</c>
/// (list) and <c>GetIdentityGroupById</c> — the Domain entity never crosses the Application
/// boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="IdentityGroupsDesc">IDENTITYGROUPS_DESC column.</param>
/// <param name="IdentityGroupsCode">IDENTITYGROUPS_CODE column.</param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="TafsiliId">TAFSILI_ID column — link to <c>TB_TAFSILI</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record IdentityGroupDto(
    Guid Id,
    string IdentityGroupsDesc,
    string? IdentityGroupsCode,
    string VahedCode,
    Guid? TafsiliId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
