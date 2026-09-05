using MediatR;

namespace Accounting.Application.BankAccounts.Commands.UpdateBankAccount;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_ACCOUNT</c> row (PUT semantics, not
/// PATCH) — the same replace-vs-patch rationale as <c>UpdateWorkShopCommand</c> applies here.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteBankAccountCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNT.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountNumber">Bank account number (required, max 15 chars).</param>
/// <param name="AccountHolder">Account holder name (required, max 80 chars).</param>
/// <param name="CardNumber">Optional bank card number (max 16 chars).</param>
/// <param name="ShebaNumber">Optional IBAN/Sheba number (max 50 chars).</param>
/// <param name="FirstAmount">Optional opening balance (<c>NUMBER(25)</c>).</param>
/// <param name="BankId">Optional link to <c>TB_BANK_LIST</c> (<c>FK_BANK_ACCOUNT</c>).</param>
/// <param name="BranchId">Optional link to <c>TB_BANKBRANCH_LIST</c> (<c>FK_BANKBRANCH_ACCOUNT</c>).</param>
/// <param name="AccountTypeId">Optional link to <c>TB_ACCOUNT_TYPE</c> — see <c>CreateBankAccountCommand</c> XML doc for the no-FK warning.</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_ACCOUNT</c>); part of <c>UK_ACCOUNT_ACCOUNTCODE</c> together with <c>VahedCode</c>.</param>
/// <param name="CheckFile">Optional Oracle BLOB; no size limit enforced here.</param>
/// <param name="VahedCode">Optional organizational unit code (max 4 chars); part of <c>UK_ACCOUNT_ACCOUNTCODE</c>.</param>
/// <param name="AccountOpeningDate">Optional opening date, Legacy string format (max 8 chars).</param>
public sealed record UpdateBankAccountCommand(
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
    byte[]? CheckFile,
    string? VahedCode,
    string? AccountOpeningDate) : IRequest;
