using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
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
/// <c>PayReciveType</c> (<c>PAYRECIVTYPE</c>) is now <see cref="PayRecivType"/> — resolved in
/// phase 27 batch 2 (previously a CONFIRMED instance of the phase-12 <c>bool?</c>-should-be-enum
/// bug; see <c>docs/centralaccount-business-reference.md</c> §24-1 row 14, formerly §10-2 row 14).
/// The reference project models this exact column as <c>PayRecivType</c> with <b>three</b> values
/// — <c>۱پرداخت ۲دریافت ۳همه</c> (1 = payment, 2 = receipt, 3 = both). Before this fix, the third
/// value was structurally <em>unreachable</em> through a <see cref="bool"/>? — this is the same
/// class of defect as <c>TB_ELAMHEAD.ELAMHDRAMAD_TYPE</c> (phase 15) and
/// <c>TB_WHITEANDBLACKLIST.STATE</c> (phase 13, since resolved in phase 27 batch 3 to
/// <see cref="WhiteBlackListState"/>) — this note fixes the CLR type for this column specifically.
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
/// <param name="PayReciveType">PAYRECIVTYPE column — <see cref="PayRecivType"/> (1=Pay, 2=Recive, 3=All). See the class XML doc for the resolved-enum note.</param>
/// <param name="Year">YEAR column (required, max 4 chars, fixed-length — fiscal year).</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column (optional) — the accounting voucher this document was turned into (<c>FK_PAYRECIV_VOCHERHEAD</c>).</param>
public sealed record CreatePayReciveHeadCommand(
    string PayReciveCode,
    string PayReciveDate,
    string PayReciveDescription,
    PayRecivType? PayReciveType,
    string Year,
    Guid? VoucherHeadId) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (required, max 4 chars — organizational unit code). Never bound from the
    /// request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and
    /// the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>CreatePayReciveHeadCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
