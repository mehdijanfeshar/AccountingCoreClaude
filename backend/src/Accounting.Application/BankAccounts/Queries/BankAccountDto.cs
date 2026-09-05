namespace Accounting.Application.BankAccounts.Queries;

/// <summary>
/// Read-side projection of <c>TB_ACCOUNT</c> (a bank account — NOT the chart-of-accounts node
/// <c>TB_ACCOUNTCODE</c>). Used by both <c>GetBankAccounts</c> (list) and
/// <c>GetBankAccountById</c> — the Domain entity never crosses the Application boundary.
///
/// ⚠️ Deliberately excludes <c>CheckFile</c> (the BLOB column): list/detail projections are not
/// the place to stream binary content — a dedicated download endpoint would be the appropriate
/// place for that, and none exists yet (out of scope for this batch).
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="AccountNumber">ACCOUNTNUMBER column.</param>
/// <param name="AccountHolder">ACCOUNTHOLDER column.</param>
/// <param name="CardNumber">CARDNUMBER column — optional.</param>
/// <param name="ShebaNumber">SHEBANUMBER column — optional.</param>
/// <param name="FirstAmount">FIRSTAMOUNT column — optional opening balance.</param>
/// <param name="BankId">BANK_ID column — optional link to <c>TB_BANK_LIST</c>.</param>
/// <param name="BranchId">BRANCH_ID column — optional link to <c>TB_BANKBRANCH_LIST</c>.</param>
/// <param name="AccountTypeId">ACCOUNTTYPE_ID column — optional; see <c>CreateBankAccountCommand</c> XML doc for the no-FK warning.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — optional link to <c>TB_ACCOUNTCODE</c>; part of <c>UK_ACCOUNT_ACCOUNTCODE</c>.</param>
/// <param name="VahedCode">VAHEDCODE column — optional; part of <c>UK_ACCOUNT_ACCOUNTCODE</c>.</param>
/// <param name="AccountOpeningDate">ACCOUNTOPENINGDATE column — optional Legacy string date.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record BankAccountDto(
    Guid Id,
    string AccountNumber,
    string AccountHolder,
    string? CardNumber,
    string? ShebaNumber,
    decimal? FirstAmount,
    Guid? BankId,
    Guid? BranchId,
    Guid? AccountTypeId,
    Guid? AccountCodeId,
    string? VahedCode,
    string? AccountOpeningDate,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
