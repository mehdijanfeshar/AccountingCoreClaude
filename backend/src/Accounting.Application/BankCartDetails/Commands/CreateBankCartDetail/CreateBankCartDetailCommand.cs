using MediatR;

namespace Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail;

/// <summary>
/// Creates a new <c>TB_BANKCARTDETAIL</c> row (Legacy bank-card/cheque-or-receipt detail line —
/// links a <c>TB_CHECK</c> or <c>TB_RECEIP</c> row to a specific bank/branch/account-number
/// movement for a given month/year). Carries primitive fields only — the handler is responsible
/// for constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>CheckId</c> is backed by a real, optional FK (<c>FK_BANKCART_CHECK</c> to <c>TB_CHECK</c>);
/// <c>ReceipId</c> is backed by a real, optional FK (<c>FK_BANKCART_RECEIP</c> to
/// <c>TB_RECEIP</c>). Both are mapped centrally to 400 by <c>UnitOfWork.SaveChangesAsync</c> on
/// violation.
///
/// ⚠️ <c>BankId</c>, <c>BranchId</c> and <c>CheckIncorrentId</c> have NO FK at all in
/// <c>LegacyDbContext</c>, despite the <c>_ID</c> suffix — an invalid value is written silently
/// and the central FK→400 mapping cannot catch it (there is no constraint to violate). No
/// pre-check is invented here, per project convention (races on FK/UNIQUE are deliberately left
/// to the database).
///
/// <b>UNIQUE constraint <c>AK_AK_BANKCARTDETAIL_BANKCART</c></b> is an unusually wide 11-column
/// composite over <c>BANK_ID, BRANCH_ID, ACCOUNTNUMBER, MONTH, CHEQNO, RECIVDATE,
/// CHECKRECEIPTTYPE, DEBTOR, CREDITOR, VAHEDCODE, YEAR</c> — every participating column is
/// nullable. Mapped centrally to 409 by <c>UnitOfWork.SaveChangesAsync</c> when it triggers.
///
/// ⚠️ <c>CheckReceiptType</c> is <c>NUMBER(1)</c> mapped as <see cref="bool"/>?, and its own
/// Oracle column comment reads «نوع مدرك بانكي (فيش يا حواله)» — i.e. a <em>kind</em>
/// (receipt-vs-transfer), not a true/false flag. This schema has a confirmed track record of
/// <c>NUMBER(1)</c> columns actually being multi-valued enums (CLAUDE.md phase 12). Flagged here,
/// not fixed — the CLR type is deliberately left as <see cref="bool"/>?; re-typing it is a
/// separate, not-yet-made decision.
///
/// ⚠️ <c>Debtor</c> and <c>Creditor</c> are both nullable and completely independent — nothing at
/// this layer enforces that exactly one is populated or that any balance holds across rows. The
/// open project risk "amount data type: <c>long</c> vs <c>decimal?</c>" applies equally here. No
/// validation rule is invented for either.
/// </summary>
/// <param name="ReceipId">Optional link to <c>TB_RECEIP</c> (<c>FK_BANKCART_RECEIP</c>).</param>
/// <param name="CheckId">Optional link to <c>TB_CHECK</c> (<c>FK_BANKCART_CHECK</c>).</param>
/// <param name="BankId">Bank identifier — NO FK exists for this column; see the class remarks.</param>
/// <param name="BranchId">Bank branch identifier — NO FK exists for this column; see the class remarks.</param>
/// <param name="AccountNumber">ACCOUNTNUMBER column (optional, max 13 chars).</param>
/// <param name="Month">MONTH column (optional, max 2 chars).</param>
/// <param name="Cheqno">CHEQNO column (optional, max 8 chars).</param>
/// <param name="RecivDate">RECIVDATE column (optional, max 8 chars).</param>
/// <param name="CheckReceiptType">CHECKRECEIPTTYPE column — see the unverified-enum note above.</param>
/// <param name="Debtor">DEBTOR column (<c>NUMBER(25)</c>, optional); see the balance note above.</param>
/// <param name="Creditor">CREDITOR column (<c>NUMBER(25)</c>, optional); see the balance note above.</param>
/// <param name="VahedCode">Organizational unit code (optional, max 4 chars).</param>
/// <param name="Year">Fiscal year (optional, max 4 chars).</param>
/// <param name="CheckIncorrentId">Prior-year in-transit identifier — NO FK exists for this column; see the class remarks.</param>
public sealed record CreateBankCartDetailCommand(
    Guid? ReceipId,
    Guid? CheckId,
    Guid? BankId,
    Guid? BranchId,
    string? AccountNumber,
    string? Month,
    string? Cheqno,
    string? RecivDate,
    bool? CheckReceiptType,
    decimal? Debtor,
    decimal? Creditor,
    string? VahedCode,
    string? Year,
    Guid? CheckIncorrentId) : IRequest<Guid>;
