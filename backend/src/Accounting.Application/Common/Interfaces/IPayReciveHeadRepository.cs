using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PAYRECIVHEAD"/> (Legacy payment/receipt document
/// header — سرسند دریافت و پرداخت). Only stages changes — it does NOT call SaveChanges; the
/// handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY. <c>TB_PAYRECIVHEAD</c>'s child <c>TB_PAYRECIVDETAIL</c> is explicitly out of
/// scope for this batch — the aggregate boundary for this Head/Detail pair has not been decided
/// (see <c>docs/open-decisions.md</c>). This repository deliberately has no method touching
/// <c>TB_PAYRECIVDETAIL</c>; it follows the phase-5..8 <c>VoucherHead</c> and phase-15
/// <c>ElamHead</c> precedent (standalone Head CRUD only, no composite create, no cascade).
/// </summary>
public interface IPayReciveHeadRepository
{
    Task AddAsync(TB_PAYRECIVHEAD payReciveHead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PAYRECIVHEAD"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_PAYRECIVHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
