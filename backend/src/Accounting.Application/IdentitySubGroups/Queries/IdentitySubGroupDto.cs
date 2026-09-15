namespace Accounting.Application.IdentitySubGroups.Queries;

/// <summary>
/// Read-side projection of <c>TB_IDENTITYSUBGRP</c>. Used by both <c>GetIdentitySubGroups</c>
/// (list) and <c>GetIdentitySubGroupById</c> — the Domain entity never crosses the Application
/// boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="IdentyGroupsId">IDENTYGROUPS_ID column — link to <c>TB_IDENTITYGROUP</c>.</param>
/// <param name="SubgrpsDesc">SUBGRPS_DESC column.</param>
/// <param name="SubgrpsLen">SUBGRPS_LEN column.</param>
/// <param name="SumFlag">SUMFLAG column.</param>
/// <param name="Fixed">FIXED column.</param>
/// <param name="SubgrpsType">
/// SUBGRPS_TYPE column — ⚠️ confirmed-suspicious three-valued column exposed as <c>bool?</c> as-is;
/// see <c>CreateIdentitySubGroupCommand.SubgrpsType</c> XML doc.
/// </param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="IdentySubGroupsCode">IDENTYSUBGROUPS_CODE column.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record IdentitySubGroupDto(
    Guid Id,
    Guid IdentyGroupsId,
    string SubgrpsDesc,
    byte SubgrpsLen,
    bool SumFlag,
    bool Fixed,
    bool? SubgrpsType,
    string VahedCode,
    string Year,
    string? IdentySubGroupsCode,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
