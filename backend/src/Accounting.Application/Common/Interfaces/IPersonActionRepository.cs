using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PERSON_ACTION"/>. Only stages changes — it does NOT
/// call SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPersonActionRepository
{
    Task AddAsync(TB_PERSON_ACTION personAction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PERSON_ACTION"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>). Returns <see langword="null"/> when no row with
    /// that <c>ID</c> exists — soft-deleted rows are still returned here.
    /// </summary>
    Task<TB_PERSON_ACTION?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
