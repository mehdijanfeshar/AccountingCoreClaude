using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BankAccounts.Commands.CreateBankAccount;

/// <summary>
/// Creates a new <c>TB_ACCOUNT</c> row (Legacy bank account master — NOT the chart-of-accounts
/// node <c>TB_ACCOUNTCODE</c>). Carries primitive fields only — the handler is responsible for
/// constructing the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>AccountCodeId</c>/<c>BankId</c>/<c>BranchId</c> are backed by real, optional FKs
/// (<c>FK_ACCOUNTCODE_ACCOUNT</c> to <c>TB_ACCOUNTCODE</c>, <c>FK_BANK_ACCOUNT</c> to
/// <c>TB_BANK_LIST</c>, <c>FK_BANKBRANCH_ACCOUNT</c> to <c>TB_BANKBRANCH_LIST</c>). All three are
/// mapped centrally to 400 by <c>UnitOfWork.SaveChangesAsync</c> on violation.
///
/// ⚠️ <c>AccountTypeId</c> has NO FK at all in the Legacy schema, even though
/// <c>TB_ACCOUNT_TYPE</c> exists — so a value that references no real row is written silently
/// and the central FK→400 mapping does not help. Flagged (not fixed) here: no pre-check is added,
/// per project convention (the same judgement already applied to UNIQUE/FK races elsewhere).
///
/// ⚠️ <c>CheckFile</c> maps to an Oracle <c>BLOB</c> with no size limit enforced at this layer.
/// An upload/request-size policy is an unmade decision — no limit is invented here.
/// </summary>
/// <param name="AccountNumber">Bank account number (required, max 15 chars).</param>
/// <param name="AccountHolder">Account holder name (required, max 80 chars).</param>
/// <param name="CardNumber">Optional bank card number (max 16 chars).</param>
/// <param name="ShebaNumber">Optional IBAN/Sheba number (max 50 chars).</param>
/// <param name="FirstAmount">Optional opening balance (<c>NUMBER(25)</c>).</param>
/// <param name="BankId">Optional link to <c>TB_BANK_LIST</c> (<c>FK_BANK_ACCOUNT</c>).</param>
/// <param name="BranchId">Optional link to <c>TB_BANKBRANCH_LIST</c> (<c>FK_BANKBRANCH_ACCOUNT</c>).</param>
/// <param name="AccountTypeId">Optional link to <c>TB_ACCOUNT_TYPE</c> — see the no-FK warning above.</param>
/// <param name="AccountCodeId">Optional link to <c>TB_ACCOUNTCODE</c> (<c>FK_ACCOUNTCODE_ACCOUNT</c>); part of <c>UK_ACCOUNT_ACCOUNTCODE</c> together with <c>VahedCode</c>.</param>
/// <param name="CheckFile">Optional Oracle BLOB; no size limit enforced here.</param>
/// <param name="AccountOpeningDate">Optional opening date, Legacy string format (max 8 chars).</param>
public sealed record CreateBankAccountCommand(
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
    string? AccountOpeningDate) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (max 4 chars); part of <c>UK_ACCOUNT_ACCOUNTCODE</c>. Although
    /// <c>TB_ACCOUNT.VAHEDCODE</c> is nullable in Legacy, this property is deliberately
    /// non-nullable <see cref="string"/> here — <c>VahedScopeBehavior</c> always assigns a real
    /// value before the handler runs, so an empty string never actually reaches
    /// <c>TB_ACCOUNT.VAHEDCODE</c> in practice. Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateBankAccountCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
