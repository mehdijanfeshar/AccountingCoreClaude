using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Manual match/unmatch of one صورت‌حساب بانکی line against a دفتری <c>TB_VOUCHERSDETAIL</c> row
/// — خزانه‌داری، بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only stages changes — never
/// calls SaveChanges.
/// </summary>
public interface IBankStatementManualMatchService
{
    /// <summary>
    /// Requires <paramref name="line"/> to be
    /// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.Unmatched"/> (409
    /// otherwise). Validates the candidate <paramref name="voucherDetailId"/> exists in
    /// <paramref name="vahedCode"/>, belongs to the statement's bank account's معین, its direction
    /// matches (debit for a deposit line, credit for a withdrawal line) and its amount equals the
    /// line's — any mismatch is 400 (<c>TreasuryBankStatementMatchMismatchException</c>). Also
    /// rejects a candidate already claimed by another live statement line (400, same exception).
    /// </summary>
    Task MatchAsync(
        TB_TR_BANK_STATEMENT statement,
        TB_TR_BANK_STATEMENT_LINE line,
        Guid voucherDetailId,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>Requires <paramref name="line"/> to be
    /// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.AutoMatched"/>/
    /// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.ManualMatched"/> (409
    /// otherwise). Clears the match back to
    /// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.Unmatched"/>.</summary>
    Task UnmatchAsync(TB_TR_BANK_STATEMENT_LINE line, CancellationToken cancellationToken = default);
}
