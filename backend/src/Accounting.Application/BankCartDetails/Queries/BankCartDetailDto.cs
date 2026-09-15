namespace Accounting.Application.BankCartDetails.Queries;

/// <summary>
/// Read-side projection of <c>TB_BANKCARTDETAIL</c>. Used by both <c>GetBankCartDetails</c>
/// (list) and <c>GetBankCartDetailById</c> — the Domain entity never crosses the Application
/// boundary. Every field mirrors the identically-named parameter documented on
/// <see cref="Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail.CreateBankCartDetailCommand"/>;
/// full FK/UNIQUE/enum/balance flags are not repeated here.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="ReceipId">RECEIP_ID column — optional link to <c>TB_RECEIP</c>.</param>
/// <param name="CheckId">CHECK_ID column — optional link to <c>TB_CHECK</c>.</param>
/// <param name="BankId">BANK_ID column — no FK (see <c>CreateBankCartDetailCommand</c>).</param>
/// <param name="BranchId">BRANCH_ID column — no FK (see <c>CreateBankCartDetailCommand</c>).</param>
/// <param name="AccountNumber">ACCOUNTNUMBER column.</param>
/// <param name="Month">MONTH column.</param>
/// <param name="Cheqno">CHEQNO column.</param>
/// <param name="RecivDate">RECIVDATE column.</param>
/// <param name="CheckReceiptType">CHECKRECEIPTTYPE column — see the unverified-enum note on <c>CreateBankCartDetailCommand</c>.</param>
/// <param name="Debtor">DEBTOR column.</param>
/// <param name="Creditor">CREDITOR column.</param>
/// <param name="VahedCode">VAHEDCODE column.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="CheckIncorrentId">CHECK_INCORRENT_ID column — no FK.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag, exposed as-is.</param>
public sealed record BankCartDetailDto(
    Guid Id,
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
    Guid? CheckIncorrentId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
