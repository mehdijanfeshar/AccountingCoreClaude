using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_REFUND"/> ("استرداد وجه تنخواه") — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>، «بخش ۳ — طراحی»). Only stages changes — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashRefundRepository
{
    Task AddAsync(TB_PC_REFUND refund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_REFUND"/> by <c>ID</c> as change-tracked, verifying unit
    /// ownership via <c>VahedOwnership</c> like every other by-id repository method in this
    /// project. Returns <see langword="null"/> when no row with that <c>ID</c> exists.
    /// </summary>
    Task<TB_PC_REFUND?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of <c>AMOUNT</c> across every non-deleted استرداد row for <paramref name="fundId"/> —
    /// the "+Σ(AMOUNT استردادهای حذف‌نشده)" term of §2's extended balance equation.
    /// </summary>
    Task<decimal> GetTotalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Highest existing numeric suffix among every <c>"REF-"</c>-prefixed <c>CODE</c> for
    /// <paramref name="vahedCode"/> (including soft-deleted rows, so a deleted استرداد's number is
    /// never handed out twice) plus one — same "highest + 1, gaps allowed" shape as
    /// <c>IChargeAndCostRepository.GetNextCodeAsync</c>.
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, CancellationToken cancellationToken = default);
}
