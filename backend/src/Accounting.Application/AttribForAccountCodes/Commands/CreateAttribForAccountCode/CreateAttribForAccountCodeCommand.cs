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
/// ATTRIBBOXNO column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). This is a candidate
/// for the known project-wide <c>bool?</c>/enum scaffolding bug documented in CLAUDE.md Phase 12
/// (19 columns across 13 tables, only a handful triaged so far) — this specific column has NOT
/// been scanned/confirmed yet, so it is modeled as-is (matching the current Domain entity) rather
/// than guessed at. Fixing the underlying CLR type is a separate, out-of-scope task.
/// </param>
/// <param name="Flag">FLAG column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). Same unverified-enum caveat as <see cref="AttribBoxNo"/>.</param>
/// <param name="LenAtr">LENATR column (<c>NUMBER(2)</c>, attribute digit length).</param>
/// <param name="AttribSum">ATTRIBSUM column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). Same unverified-enum caveat as <see cref="AttribBoxNo"/>.</param>
/// <param name="ControlId">
/// CONTROLID column. Odd mapping in <c>LegacyDbContext</c>: <c>.IsRequired()</c> combined with
/// <c>HasDefaultValueSql("null ")</c>, while the CLR property is <c>bool?</c> — passed through
/// verbatim here without attempting to resolve that oddity (pre-existing, not introduced by this
/// command). Same unverified-enum caveat as <see cref="AttribBoxNo"/>.
/// </param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, required — participates in <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c>).</param>
/// <param name="Year">YEAR column (max 4 chars, required — participates in <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c>).</param>
public sealed record CreateAttribForAccountCodeCommand(
    Guid AccountCodeId,
    bool AttribBoxNo,
    bool Flag,
    byte LenAtr,
    bool AttribSum,
    bool? ControlId,
    string VahedCode,
    string Year) : IRequest<Guid>;
