using MediatR;

namespace Accounting.Application.PayReciveHeads.Commands.CreatePayReciveHead;

/// <summary>
/// Creates a new <c>TB_PAYRECIVHEAD</c> row (Legacy payment/receipt document header —
/// سرسند دریافت و پرداخت). Carries primitive fields only — the handler is responsible for
/// constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// ⚠️ <b>HEAD ONLY.</b> <c>TB_PAYRECIVHEAD</c>'s child <c>TB_PAYRECIVDETAIL</c> is explicitly
/// out of scope — the aggregate boundary for this Head/Detail pair has not been decided (see
/// <c>docs/open-decisions.md</c>). This command follows the phase-5..8 <c>VoucherHead</c> and
/// phase-15 <c>ElamHead</c> precedent exactly: standalone Head CRUD only, no composite create,
/// no cascade. See <c>Accounting.Api.Controllers.PayReciveHeadsController</c> XML doc for the
/// explicit statement that this is deliberate, not an oversight.
///
/// <c>VoucherHeadId</c> is backed by a real, optional FK (<c>FK_PAYRECIV_VOCHERHEAD</c> to
/// <c>TB_VOUCHERSHEAD</c>), mapped centrally to 400 by <c>UnitOfWork.SaveChangesAsync</c> on
/// violation. It is the table's <em>only</em> FK.
///
/// ⚠️⚠️ <b><c>PayReciveType</c> (<c>PAYRECIVTYPE</c>) is a CONFIRMED instance of the phase-12
/// <c>bool?</c>-should-be-enum bug — flagged, NOT fixed</b> (re-typing would be a breaking
/// API-contract change, out of scope for this batch). Per
/// <c>docs/centralaccount-business-reference.md</c> §10-2 row 14, the reference project models
/// this exact column as <c>PayRecivType</c> with <b>three</b> values — <c>۱پرداخت ۲دریافت ۳همه</c>
/// (1 = payment, 2 = receipt, 3 = both) — and the reference table marks our <c>bool?</c> mapping
/// 🔴 <b>غلط</b> outright. Consequences while it stays <see cref="bool"/>?: the third value is
/// <em>unreachable</em> through this API, and an existing row holding 2 is very likely read back
/// as <see langword="true"/> exactly like a row holding 1. This is the same class of defect as
/// <c>TB_ELAMHEAD.ELAMHDRAMAD_TYPE</c> (phase 15) and <c>TB_WHITEANDBLACKLIST.STATE</c>
/// (phase 13).
///
/// ⚠️ <b>No duplicate-code guard is implemented, deliberately.</b> The reference project's
/// <c>AddPayReciveCommand</c>/<c>UpdatePayReciveCommand</c> both carry a "duplicate number"
/// guard (<c>docs/centralaccount-business-reference.md</c> §21-6), but <c>TB_PAYRECIVHEAD</c>
/// has <b>no UNIQUE constraint at all</b> in <c>LegacyDbContext</c>. Re-creating that guard here
/// would be inventing a business rule with no schema backing (and would be race-prone besides),
/// which the recorded "Legacy fully replaces the rich model" decision forbids. Two headers with
/// the same <c>PayReciveCode</c> are therefore accepted, and 409 is never declared.
/// </summary>
/// <param name="PayReciveCode">PAYRECIVCODE column (required, max 5 chars — document number). ⚠️ NOT unique: no UNIQUE constraint exists on this table.</param>
/// <param name="PayReciveDate">PAYRECIVDATE column (required, max 8 chars — Legacy string-encoded date, not a real <see cref="DateTime"/>).</param>
/// <param name="PayReciveDescription">PAYRECIVDESCRIPTION column (required, max 250 chars — free-text description; the Oracle column carries <c>DEFAULT '-'</c>, but this command always sends an explicit value).</param>
/// <param name="PayReciveType">PAYRECIVTYPE column (optional <see cref="bool"/>) — ⚠️ see the CONFIRMED enum flag above; real values are 1/2/3, so the third is unreachable via <see cref="bool"/>?.</param>
/// <param name="VahedCode">VAHEDCODE column (required, max 4 chars — organizational unit code).</param>
/// <param name="Year">YEAR column (required, max 4 chars, fixed-length — fiscal year).</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column (optional) — the accounting voucher this document was turned into (<c>FK_PAYRECIV_VOCHERHEAD</c>).</param>
public sealed record CreatePayReciveHeadCommand(
    string PayReciveCode,
    string PayReciveDate,
    string PayReciveDescription,
    bool? PayReciveType,
    string VahedCode,
    string Year,
    Guid? VoucherHeadId) : IRequest<Guid>;
