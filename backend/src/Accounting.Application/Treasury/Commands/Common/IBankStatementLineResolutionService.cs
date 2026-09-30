using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Resolves/unresolves one <see cref="BankStatementLineMatchState.Unmatched"/>/
/// <see cref="BankStatementLineMatchState.Resolved"/> صورت‌حساب بانکی line without an underlying
/// دفتری counterpart — خزانه‌داری، بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only
/// stages changes — never calls SaveChanges; commands that end up issuing a GL voucher
/// (<see cref="BankStatementLineResolutionType.BankFeeVoucher"/>) still need the caller to own the
/// transaction boundary via <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public interface IBankStatementLineResolutionService
{
    /// <summary>
    /// Requires <paramref name="line"/> to be <see cref="BankStatementLineMatchState.Unmatched"/>
    /// (409 otherwise). <paramref name="type"/> rules (400 <c>TreasuryBankStatementResolutionInvalidException</c>
    /// on any violation):
    /// <list type="bullet">
    /// <item><description><see cref="BankStatementLineResolutionType.BankFeeVoucher"/> — only on a
    /// withdrawal line (<c>WITHDRAWAL &gt; 0</c>); issues the two-line temporary GL voucher via
    /// <see cref="IBankFeeVoucherBuilder"/>.</description></item>
    /// <item><description><see cref="BankStatementLineResolutionType.LinkedReceipt"/> — only on a
    /// deposit line (<c>DEPOSIT &gt; 0</c>); <paramref name="receiptId"/> required and must be a
    /// <see cref="Accounting.Domain.ValueObjects.ReceiptState.Registered"/> دریافت of the same bank
    /// account and amount.</description></item>
    /// <item><description><see cref="BankStatementLineResolutionType.Ignored"/> — <paramref name="note"/>
    /// required (non-empty).</description></item>
    /// </list>
    /// </summary>
    Task ResolveAsync(
        TB_TR_BANK_STATEMENT statement,
        TB_TR_BANK_STATEMENT_LINE line,
        BankStatementLineResolutionType type,
        Guid? receiptId,
        string? note,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requires <paramref name="line"/> to be <see cref="BankStatementLineMatchState.Resolved"/>
    /// (409 otherwise). For <see cref="BankStatementLineResolutionType.BankFeeVoucher"/>, the
    /// resolution voucher must still be <c>DocLife.Temporary</c> — otherwise
    /// <c>TreasuryBankStatementUnresolveNotAllowedException</c> (409); when allowed, the voucher is
    /// soft-deleted (head + detail tree) the same way <c>DeleteVoucherHeadCommandHandler</c> would.
    /// Other resolution types just clear the resolution fields.
    /// </summary>
    Task UnresolveAsync(TB_TR_BANK_STATEMENT_LINE line, string vahedCode, CancellationToken cancellationToken = default);
}
