using MediatR;

namespace Accounting.Application.IdentitySubGroups.Commands.UpdateIdentitySubGroup;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_IDENTITYSUBGRP</c> row (PUT
/// semantics, not PATCH). Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c>
/// and <c>ISDELETED</c>: identity and creation audit are immutable after insert, and
/// <c>ISDELETED</c> is owned exclusively by <c>DeleteIdentitySubGroupCommand</c>.
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent because the handler sources them
/// from <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYSUBGRP.ID</c> to update (bound from the route, never the body).</param>
/// <param name="IdentyGroupsId">IDENTYGROUPS_ID column — required FK to <c>TB_IDENTITYGROUP</c>.</param>
/// <param name="SubgrpsDesc">SUBGRPS_DESC column (max 100 chars, required).</param>
/// <param name="SubgrpsLen">SUBGRPS_LEN column (<c>NUMBER(2)</c>, mapped as <c>byte</c>, required).</param>
/// <param name="SumFlag">SUMFLAG column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>).</param>
/// <param name="Fixed">FIXED column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>).</param>
/// <param name="SubgrpsType">
/// SUBGRPS_TYPE column — ⚠️ CONFIRMED-SUSPICIOUS three-valued column modeled as <c>bool?</c>; see
/// the identical note on <c>CreateIdentitySubGroupCommand.SubgrpsType</c> for full detail.
/// </param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, required).</param>
/// <param name="Year">YEAR column (max 4 chars, required fiscal year).</param>
/// <param name="IdentySubGroupsCode">IDENTYSUBGROUPS_CODE column (max 2 chars, optional).</param>
public sealed record UpdateIdentitySubGroupCommand(
    Guid Id,
    Guid IdentyGroupsId,
    string SubgrpsDesc,
    byte SubgrpsLen,
    bool SumFlag,
    bool Fixed,
    bool? SubgrpsType,
    string VahedCode,
    string Year,
    string? IdentySubGroupsCode) : IRequest;
