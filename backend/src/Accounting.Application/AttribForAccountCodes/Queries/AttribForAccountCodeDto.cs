using Accounting.Domain.ValueObjects;

namespace Accounting.Application.AttribForAccountCodes.Queries;

/// <summary>
/// Read-side projection of <c>TB_ATTRIBFORACCOUNTCODE</c> (per-account-code identification-digit
/// attribute definition). Used by both <c>GetAttribForAccountCodes</c> (list) and
/// <c>GetAttribForAccountCodeById</c> — the Domain entity never crosses the Application boundary.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — link to <c>TB_ACCOUNTCODE</c> (<c>FK_ATTRIBFO_ACCOUNTCODE</c>).</param>
/// <param name="MoinCode">
/// <c>ACCCODE</c> of the linked account, projected through the required <c>ACCOUNTCODE</c>
/// navigation. Denormalized on purpose: the list this DTO feeds is "حساب‌های شناسه‌دار", so the
/// معین is its defining column — returning only the opaque <see cref="Guid"/> forced the client
/// into an N+1 lookup just to render a row. Nullable because <c>TB_ACCOUNTCODE.ACCCODE</c> is
/// itself nullable in Legacy, not because the link can be missing (the FK is required).
/// </param>
/// <param name="MoinName">
/// <c>ACCCODENAME</c> of the linked account — same rationale as <paramref name="MoinCode"/>.
/// </param>
/// <param name="AttribBoxNo">
/// ATTRIBBOXNO column (<c>NUMBER(1)</c>) — plain <see cref="short"/> (NOT an enum), resolved in
/// phase 27 batch 2 from an incorrect non-nullable <c>bool</c>; see
/// <c>docs/centralaccount-business-reference.md</c> §24-1.
/// </param>
/// <param name="Flag">
/// FLAG column — <see cref="AttribFlag"/> (1=Number, 2=Date). Resolved in phase 27 batch 2 from
/// an incorrect non-nullable <c>bool</c>; see <c>docs/centralaccount-business-reference.md</c>
/// §24-1.
/// </param>
/// <param name="LenAtr">LENATR column (<c>NUMBER(2)</c>, attribute digit length).</param>
/// <param name="AttribSum">
/// ATTRIBSUM column — <see cref="ValueObjects.AttribSum"/> (1=Summable, 2=UnSummable). Same
/// phase-27-batch-2 fix as <see cref="Flag"/>.
/// </param>
/// <param name="ControlId">
/// CONTROLID column — <see cref="AttribControl"/>? in the CLR model despite the Fluent mapping
/// marking it <c>.IsRequired()</c> with <c>HasDefaultValueSql("null ")</c> (an existing oddity in
/// <c>LegacyDbContext</c>, not introduced here). Same phase-27-batch-2 fix as <see cref="Flag"/>.
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
    string? MoinCode,
    string? MoinName,
    short AttribBoxNo,
    AttribFlag Flag,
    byte LenAtr,
    AttribSum AttribSum,
    AttribControl? ControlId,
    string VahedCode,
    string Year,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
