using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_REPLENISHMENT"/> ("ترمیم/شارژ تنخواه") — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>، «بخش ۳ — طراحی»). Only stages changes — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashReplenishmentRepository
{
    Task AddAsync(TB_PC_REPLENISHMENT replenishment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_REPLENISHMENT"/> by <c>ID</c> as change-tracked, verifying
    /// unit ownership via <c>VahedOwnership</c> like every other by-id repository method in this
    /// project. Returns <see langword="null"/> when no row with that <c>ID</c> exists.
    /// </summary>
    Task<TB_PC_REPLENISHMENT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of <c>TOTAL_AMOUNT</c> across every non-deleted <see cref="PettyCashReplenishmentState.Paid"/>
    /// row for <paramref name="fundId"/> — the "+Σ(TOTAL_AMOUNT ترمیم‌های Paid)" term of §2's
    /// extended balance equation (<see cref="Accounting.Application.PettyCash.Common.PettyCashBalanceCalculator"/>).
    /// </summary>
    Task<decimal> GetPaidTotalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default);
}
