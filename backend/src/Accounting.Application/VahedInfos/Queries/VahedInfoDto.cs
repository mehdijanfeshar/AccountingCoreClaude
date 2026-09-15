namespace Accounting.Application.VahedInfos.Queries;

/// <summary>
/// Read-side projection of <c>TB_VAHED_INFO</c>. Used by both <c>GetVahedInfos</c> (list) and
/// <c>GetVahedInfoById</c> — the Domain entity never crosses the Application boundary.
///
/// Deliberately has NO audit fields and NO <c>IsDeleted</c> field: <c>TB_VAHED_INFO</c> has no
/// <c>ADDUSERID</c>/<c>CHANGEUSERID</c>/<c>CREATEDDATE</c>/<c>UPDATEDDATE</c>/<c>ISDELETED</c>
/// column at all (locked in by
/// <c>Accounting.Application.Tests.VahedInfos.VahedInfoSchemaAssumptionsTests</c>).
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="VahedCode">VAHEDCODE column — organizational unit code (unique — <c>UK_VAHEDINFO</c>).</param>
/// <param name="VahedName">VAHEDNAME column — organizational unit name.</param>
/// <param name="CityId">CITY_ID column — link to <c>TB_CITY</c> (no mapped FK constraint).</param>
/// <param name="VahedTypeId">VAHEDTYPE_ID column — link to <c>TB_VAHED_TYPE</c> (<c>FK_VAHEDINFO_TYPE</c>).</param>
/// <param name="ParentId">PARENT_ID column — optional parent unit in the self-referencing org-unit hierarchy.</param>
public sealed record VahedInfoDto(
    Guid Id,
    string VahedCode,
    string VahedName,
    Guid CityId,
    Guid VahedTypeId,
    Guid? ParentId);
