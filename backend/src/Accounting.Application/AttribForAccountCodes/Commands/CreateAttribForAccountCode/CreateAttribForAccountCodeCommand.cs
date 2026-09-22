using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;

/// <summary>
/// Creates a new <c>TB_ATTRIBFORACCOUNTCODE</c> row (Legacy per-account-code identification-digit
/// attribute definition). Carries primitive fields only — the handler is responsible for
/// constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: the combination <c>(AccountCodeId, VahedCode, Year)</c> is enforced by the Oracle
/// constraint <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c>. No pre-check is performed here — a duplicate
/// combination surfaces as an Oracle ORA-00001, translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409. A
/// non-existent <see cref="AccountCodeId"/> violates <c>FK_ATTRIBFO_ACCOUNTCODE</c> and is mapped
/// centrally to <see cref="Accounting.Application.Common.Exceptions.ForeignKeyViolationException"/>
/// → 400 — see <c>Accounting.Api.Controllers.AttribForAccountCodesController</c> XML doc.
/// </summary>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_ATTRIBFO_ACCOUNTCODE</c>).</param>
/// <param name="AttribBoxNo">
/// ATTRIBBOXNO column (<c>NUMBER(1)</c>). Retyped from non-nullable <c>bool</c> to <see cref="short"/>
/// in phase 27 batch 2 — this is NOT an enum, it is a plain count/box-number (the reference
/// project models it as <c>int AttribBoxNo</c>; see
/// <c>docs/centralaccount-business-reference.md</c> §24-1, "بدترین مورد این پاس — اصلاً enum
/// نیست، عدد است"). Range is bounded to <c>0..9</c> by the validator, reflecting the column's
/// physical <c>NUMBER(1)</c> width — not a known business rule.
/// </param>
/// <param name="Flag">FLAG column — <see cref="AttribFlag"/> (1=Number, 2=Date). Resolved from the project-wide <c>bool?</c>/enum scaffolding bug (CLAUDE.md open risk #2) in phase 27 batch 2.</param>
/// <param name="LenAtr">LENATR column (<c>NUMBER(2)</c>, attribute digit length).</param>
/// <param name="AttribSum">ATTRIBSUM column — <see cref="ValueObjects.AttribSum"/> (1=Summable, 2=UnSummable). Same phase-27-batch-2 fix as <see cref="Flag"/>.</param>
/// <param name="ControlId">
/// CONTROLID column — <see cref="AttribControl"/>? (1=NotZero, 2=IsDate). Same phase-27-batch-2
/// fix as <see cref="Flag"/>. Odd mapping in <c>LegacyDbContext</c> unchanged by this fix:
/// <c>.IsRequired()</c> combined with <c>HasDefaultValueSql("null ")</c>, while the CLR property
/// stays nullable — passed through verbatim here without attempting to resolve that pre-existing
/// oddity.
/// </param>
/// <param name="Year">YEAR column (max 4 chars, required — participates in <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c>).</param>
public sealed record CreateAttribForAccountCodeCommand(
    Guid AccountCodeId,
    short AttribBoxNo,
    AttribFlag Flag,
    byte LenAtr,
    AttribSum AttribSum,
    AttribControl? ControlId,
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required — participates in
    /// <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c>). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateAttribForAccountCodeCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
