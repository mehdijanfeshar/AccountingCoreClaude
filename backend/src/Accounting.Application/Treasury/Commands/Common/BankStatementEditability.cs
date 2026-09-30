using Accounting.Application.Common.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// The single place «آیا این صورت‌حساب بانکی هنوز قابل‌ویرایش است؟» lives — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Header/line CRUD, match/unmatch, resolve/
/// unresolve and auto-match all require <see cref="BankStatementState.Open"/>; same "one rule,
/// one home" shape as <c>VoucherEditability</c>.
/// </summary>
public static class BankStatementEditability
{
    public static void EnsureOpen(Guid statementId, BankStatementState state)
    {
        if (state != BankStatementState.Open)
        {
            throw new TreasuryBankStatementStateConflictException(statementId, state, "باز");
        }
    }

    /// <summary>Used only by <c>ReopenBankStatementCommandHandler</c> — the one action that
    /// requires <see cref="BankStatementState.Closed"/> instead of <see cref="BankStatementState.Open"/>.</summary>
    public static void EnsureClosed(Guid statementId, BankStatementState state)
    {
        if (state != BankStatementState.Closed)
        {
            throw new TreasuryBankStatementStateConflictException(statementId, state, "بسته");
        }
    }
}
