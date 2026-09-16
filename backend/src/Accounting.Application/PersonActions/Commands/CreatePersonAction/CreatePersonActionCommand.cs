using Accounting.Domain.ValueObjects;
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
/// <param name="Status">
/// STATUS column (<c>NUMBER(1)</c>, mapped as nullable <c>bool</c>). Confirmed genuinely boolean
/// — <c>docs/centralaccount-business-reference.md</c> §24-1 marks it ✅ درست and the reference
/// entity declares it <c>bool?</c> too. Left untouched.
/// </param>
/// <param name="OperatorRole">
/// OPERATORROLE column (<c>NUMBER(1)</c>, mapped as non-nullable <see cref="Accounting.Domain.ValueObjects.OperatorRole"/>).
/// Resolved per <c>docs/centralaccount-business-reference.md</c> §24-1 (phase 27 batch 3) —
/// previously an incorrect <c>bool</c>; see the historical note preserved in the open risk #2
/// entry of CLAUDE.md. 🔴 Note that <c>TB_PERSON_ACTION</c> as a whole remains completely
/// unprotected (open risk #1-ب) — this field's type is fixed here, but no authorization is added.
/// </param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, optional organizational unit code).</param>
public sealed record CreatePersonActionCommand(
    string? UserName,
    string UserId,
    string? FromDate,
    string? ToDate,
    bool? Status,
    OperatorRole OperatorRole,
    string? VahedCode) : IRequest<Guid>;
