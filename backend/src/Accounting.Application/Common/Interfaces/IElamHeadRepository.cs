using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_ELAMHEAD"/> (Legacy announcement/notice header —
/// اعلاميه). Only stages changes — it does NOT call SaveChanges; the handler owns the
/// transaction boundary via <see cref="IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY. <c>TB_ELAMHEAD</c>'s child <c>TB_ELAMDETAIL</c> is explicitly out of scope for
/// this batch — the aggregate boundary for this Head/Detail pair has not been decided (see
/// <c>docs/open-decisions.md</c>). This repository deliberately has no method touching
/// <c>TB_ELAMDETAIL</c>; follow the phase-5..8 <c>VoucherHead</c> precedent (standalone Head
/// CRUD only, no composite create, no cascade).
/// </summary>
public interface IElamHeadRepository
{
    Task AddAsync(TB_ELAMHEAD elamHead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_ELAMHEAD"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_ELAMHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
