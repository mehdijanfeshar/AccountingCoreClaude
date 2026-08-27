using MediatR;

namespace Accounting.Application.PersonActions.Commands.CreatePersonAction;

/// <summary>
/// Creates a new <c>TB_PERSON_ACTION</c> row (a legacy per-user action/session grant window).
/// Carries primitive fields only — the handler is responsible for constructing the Domain
/// entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// Uniqueness: the combination <c>(UserId, FromDate, ToDate)</c> is enforced by the Oracle
/// constraint <c>UK_PERSON_ACTION</c>. No pre-check is performed here — a duplicate combination
/// surfaces as an Oracle ORA-00001, already translated centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> into a
/// <see cref="Accounting.Application.Common.Exceptions.DuplicateKeyException"/> → 409, exactly
/// like <c>UK_ACCOUNTCODE</c>/<c>UK_VOUCHERHEAD_NUMBER</c> on the other two UNIQUE-constrained
/// tables in this project.
/// </summary>
/// <param name="UserName">USERNAME column (max 30 chars, optional).</param>
/// <param name="UserId">USERID column (max 10 chars, required — participates in <c>UK_PERSON_ACTION</c>).</param>
/// <param name="FromDate">FROMDATE column (max 8 chars — a Persian date string, participates in <c>UK_PERSON_ACTION</c>).</param>
/// <param name="ToDate">TODATE column (max 8 chars — a Persian date string, participates in <c>UK_PERSON_ACTION</c>).</param>
/// <param name="Status">STATUS column (<c>NUMBER(1)</c>, mapped as nullable <c>bool</c>).</param>
/// <param name="OperatorRole">
/// OPERATORROLE column (<c>NUMBER(1)</c>, mapped as non-nullable <c>bool</c>). NOTE: this column
/// is suspected to actually be a multi-valued enum (≈ <c>OperatorRole</c> 1..4) rather than a
/// true boolean — it matches the shape of the <c>bool?</c>/<c>NUMBER(1)</c> bug pattern CLAUDE.md's
/// Phase 12 documents (19 columns across 13 tables, only `TB_ACCOUNTCODE.TYPECODE`,
/// `TB_VOUCHERSHEAD.DOCLIFE`, and `TB_TAFSILI.ISACTIVE` triaged so far), but this specific column
/// has NOT itself been scanned/confirmed yet — no decision is recorded for it. Modeled as
/// <c>bool</c> exactly as the current Domain entity declares it — confirming and fixing the
/// underlying type is a separate, out-of-scope task and was deliberately not guessed here.
/// </param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, optional organizational unit code).</param>
public sealed record CreatePersonActionCommand(
    string? UserName,
    string UserId,
    string? FromDate,
    string? ToDate,
    bool? Status,
    bool OperatorRole,
    string? VahedCode) : IRequest<Guid>;
