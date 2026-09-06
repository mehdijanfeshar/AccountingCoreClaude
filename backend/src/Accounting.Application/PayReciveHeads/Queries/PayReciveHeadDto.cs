namespace Accounting.Application.PayReciveHeads.Queries;

/// <summary>
/// Read-side projection of <c>TB_PAYRECIVHEAD</c>. Used by both <c>GetPayReciveHeads</c> (list)
/// and <c>GetPayReciveHeadById</c> — the Domain entity never crosses the Application boundary.
///
/// ⚠️ HEAD ONLY — carries no <c>TB_PAYRECIVDETAIL</c> data (out of scope; see
/// <c>Accounting.Api.Controllers.PayReciveHeadsController</c> XML doc). In particular it carries
/// no debit/credit totals, so a caller cannot tell from this DTO whether the underlying document
/// balances.
///
/// ⚠️⚠️ <c>PayReciveType</c> is exposed as-is (<see cref="bool"/>?) — see
/// <c>CreatePayReciveHeadCommand</c> XML doc for the CONFIRMED <c>bool?</c>-should-be-enum bug on
/// that column (real values 1 = payment, 2 = receipt, 3 = both); re-typing here would be a
/// breaking contract change, deliberately not done.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="PayReciveCode">PAYRECIVCODE column — document number. ⚠️ NOT unique: this table has no UNIQUE constraint.</param>
/// <param name="PayReciveDate">PAYRECIVDATE column — Legacy string-encoded date.</param>
/// <param name="PayReciveDescription">PAYRECIVDESCRIPTION column.</param>
/// <param name="PayReciveType">PAYRECIVTYPE column — ⚠️ CONFIRMED <see cref="bool"/>?-should-be-enum, see <c>CreatePayReciveHeadCommand</c> XML doc.</param>
/// <param name="VahedCode">VAHEDCODE column — organizational unit code.</param>
/// <param name="Year">YEAR column — fiscal year.</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column — the accounting voucher this document was turned into, if any.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record PayReciveHeadDto(
    Guid Id,
    string PayReciveCode,
    string PayReciveDate,
    string PayReciveDescription,
    bool? PayReciveType,
    string VahedCode,
    string Year,
    Guid? VoucherHeadId,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
