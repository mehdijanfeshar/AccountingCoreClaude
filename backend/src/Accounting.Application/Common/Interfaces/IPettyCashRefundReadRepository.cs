using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_PC_REFUND"/> — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>، «بخش ۳ — طراحی»). Never stages changes; always
/// returns DTO projections, never the Domain entity.
/// </summary>
public interface IPettyCashRefundReadRepository
{
    /// <summary>
    /// <c>GET funds/{fundId}/refunds</c> — every non-deleted استرداد row, newest-first. Not
    /// paginated — mirrors <c>IPettyCashFundReviewerReadRepository.GetActiveAsync</c>'s
    /// unpaged-per-fund shape (a fund's refund history is expected to stay small).
    /// </summary>
    Task<IReadOnlyList<PettyCashRefundDto>> GetByFundAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>Read-side twin of <c>IPettyCashRefundRepository.GetTotalAsync</c> — same
    /// deliberate read/write query duplication as every other petty-cash balance input (see
    /// <c>IPettyCashExpenseDocRepository.GetApprovedUnlinkedByFundAsync</c> XML doc). Used by the
    /// دشبورد/گردش تنخواه Query handlers, which may not depend on write repositories.</summary>
    Task<decimal> GetTotalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default);
}
