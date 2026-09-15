namespace Accounting.Application.AttribForAccountCodes.Queries;

/// <summary>
/// Read-side projection of <c>TB_ATTRIBFORACCOUNTCODE</c> (per-account-code identification-digit
/// attribute definition). Used by both <c>GetAttribForAccountCodes</c> (list) and
/// <c>GetAttribForAccountCodeById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — link to <c>TB_ACCOUNTCODE</c> (<c>FK_ATTRIBFO_ACCOUNTCODE</c>).</param>
/// <param name="AttribBoxNo">
/// ATTRIBBOXNO column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). Unverified against
/// the CLAUDE.md Phase 12 <c>bool?</c>/enum scaffolding-bug pattern — modeled as-is.
/// </param>
/// <param name="Flag">
/// FLAG column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). Same unverified-enum
/// caveat as <see cref="AttribBoxNo"/>.
/// </param>
/// <param name="LenAtr">LENATR column (<c>NUMBER(2)</c>, attribute digit length).</param>
/// <param name="AttribSum">
/// ATTRIBSUM column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). Same unverified-enum
/// caveat as <see cref="AttribBoxNo"/>.
/// </param>
/// <param name="ControlId">
/// CONTROLID column — <c>bool?</c> in the CLR model despite the Fluent mapping marking it
/// <c>.IsRequired()</c> with <c>HasDefaultValueSql("null ")</c> (an existing oddity in
/// <c>LegacyDbContext</c>, not introduced here). Same unverified-enum caveat as
/// <see cref="AttribBoxNo"/>.
/// </param>
/// <param name="VahedCode">VAHEDCODE column — organizational unit code.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag. Exposed as-is.</param>
public sealed record AttribForAccountCodeDto(
    Guid Id,
    Guid AccountCodeId,
    bool AttribBoxNo,
    bool Flag,
    byte LenAtr,
    bool AttribSum,
    bool? ControlId,
    string VahedCode,
    string Year,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
