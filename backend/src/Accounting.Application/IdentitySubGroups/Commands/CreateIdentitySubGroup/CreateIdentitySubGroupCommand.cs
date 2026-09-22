using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;

/// <summary>
/// Creates a new <c>TB_IDENTITYSUBGRP</c> row (Legacy "identity"/شناسنامه sub-group definition,
/// mapped to Oracle table <c>TB_IDENTITYSUBGRPS</c> — note the singular CLR type name vs. the
/// plural table name). Carries primitive fields only — the handler is responsible for
/// constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: the combination <c>(VahedCode, Year, IdentySubGroupsCode)</c> is enforced by the
/// Oracle constraint <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>. No pre-check is performed here — a
/// duplicate combination surfaces as an Oracle ORA-00001, already translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409.
/// </summary>
/// <param name="IdentyGroupsId">
/// IDENTYGROUPS_ID column (note the missing "I" — the real Oracle column name is spelled this
/// way, intentionally preserved rather than silently corrected) — required FK to
/// <c>TB_IDENTITYGROUP</c> (constraint <c>FK_IDENTYSU_IDENTYGR</c>). No pre-check is performed:
/// an id that does not reference an existing <c>TB_IDENTITYGROUP</c> row surfaces as an Oracle
/// ORA-02291, already translated centrally by <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/> → 400.
/// </param>
/// <param name="SubgrpsDesc">SUBGRPS_DESC column (max 100 chars, required).</param>
/// <param name="SubgrpsLen">SUBGRPS_LEN column (<c>NUMBER(2)</c>, mapped as <c>byte</c>, required).</param>
/// <param name="SumFlag">SUMFLAG column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>).</param>
/// <param name="Fixed">
/// FIXED column — <see cref="IdentitySubGroupKind"/> (1=Fixed/ثابت, 2=Variable/متغیر). Resolved
/// from the project-wide <c>bool</c>/enum scaffolding bug (CLAUDE.md open risk #2) — see
/// <c>docs/centralaccount-business-reference.md</c> §24-1.
/// </param>
/// <param name="SubgrpsType">
/// SUBGRPS_TYPE column — <see cref="IdentitySubGroupType"/> (1=Date/تاریخ,
/// 2=PersianLetter/حروف فارسی, 3=Number/عدد, 4=LatinLetter/حروف لاتین). ⚠️ Four-valued: the
/// previous <c>bool?</c> mapping could only ever express two of these four real values (plus
/// NULL) — values 3 and 4 were structurally unreachable through this API before this fix.
/// Resolved from the project-wide <c>bool?</c>/enum scaffolding bug (CLAUDE.md open risk #2) —
/// see <c>docs/centralaccount-business-reference.md</c> §24-1.
/// </param>
/// <param name="Year">
/// YEAR column (max 4 chars, required fiscal year — participates in
/// <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>).
/// </param>
/// <param name="IdentySubGroupsCode">
/// IDENTYSUBGROUPS_CODE column (max 2 chars, optional — participates in
/// <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>).
/// </param>
public sealed record CreateIdentitySubGroupCommand(
    Guid IdentyGroupsId,
    string SubgrpsDesc,
    byte SubgrpsLen,
    bool SumFlag,
    IdentitySubGroupKind Fixed,
    IdentitySubGroupType? SubgrpsType,
    string Year,
    string? IdentySubGroupsCode) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required — participates in
    /// <c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c>). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateIdentitySubGroupCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
