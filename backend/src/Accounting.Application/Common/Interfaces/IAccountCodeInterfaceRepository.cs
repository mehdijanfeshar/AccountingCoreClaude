using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_ACCOUNTCODE_INTERFACE"/>. Only stages changes — it
/// does NOT call SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IAccountCodeInterfaceRepository
{
    Task AddAsync(TB_ACCOUNTCODE_INTERFACE accountCodeInterface, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_ACCOUNTCODE_INTERFACE"/> by <c>ID</c> as a change-tracked
    /// entity (deliberately no <c>AsNoTracking()</c>) so that Update/Delete handlers can mutate
    /// the returned instance in place. Returns <see langword="null"/> when no row with that
    /// <c>ID</c> exists — soft-deleted rows are still returned here.
    /// </summary>
    Task<TB_ACCOUNTCODE_INTERFACE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
