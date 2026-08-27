using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_BILL_LOG"/>. Only stages changes — it does NOT call
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IBillLogRepository
{
    Task AddAsync(TB_BILL_LOG billLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_BILL_LOG"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>). Returns <see langword="null"/> when no row with
    /// that <c>ID</c> exists — soft-deleted rows are still returned here.
    /// </summary>
    Task<TB_BILL_LOG?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
