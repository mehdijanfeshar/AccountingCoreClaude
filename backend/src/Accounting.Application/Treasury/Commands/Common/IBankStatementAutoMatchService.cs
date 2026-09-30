using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Auto-matches every <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.Unmatched"/>
/// line of one صورت‌حساب بانکی against candidate دفتری (book) <c>TB_VOUCHERSDETAIL</c> lines —
/// خزانه‌داری، بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). See
/// <c>BankStatementAutoMatchService</c> XML doc for the exact priority rules (owner-specified).
/// Only stages changes (line <c>MATCH_STATE</c>/<c>MATCHED_VOUCHERDETAIL_ID</c> updates) — never
/// calls SaveChanges.
/// </summary>
public interface IBankStatementAutoMatchService
{
    Task<BankStatementAutoMatchResult> AutoMatchAsync(
        TB_TR_BANK_STATEMENT statement, string vahedCode, CancellationToken cancellationToken = default);
}
