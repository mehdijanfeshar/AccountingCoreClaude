using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_BANK_STATEMENT"/> ("صورت‌حساب بانکی") — خزانه‌داری،
/// بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only stages changes — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>. Same
/// shape as <c>ITreasuryReceiptRepository</c>.
/// </summary>
public interface ITreasuryBankStatementRepository
{
    Task AddAsync(TB_TR_BANK_STATEMENT statement, CancellationToken cancellationToken = default);

    /// <summary>Loads a single statement by <c>ID</c>, change-tracked, scoped to
    /// <paramref name="vahedCode"/> via <c>VahedOwnership</c>. Returns <see langword="null"/> when
    /// no row with that <c>ID</c> exists.</summary>
    Task<TB_TR_BANK_STATEMENT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Next value of the per-(VAHEDCODE, YEAR) 6-digit counter behind <c>CODE</c> ("BST-" + this
    /// value, zero-padded) — same client-side-parsed-MAX approach as
    /// <c>ITreasuryReceiptRepository.GetNextCodeAsync</c>.
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default);
}
