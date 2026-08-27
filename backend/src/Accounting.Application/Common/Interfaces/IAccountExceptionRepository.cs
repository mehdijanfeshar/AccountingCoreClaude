using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_ACCOUNTEXCEPTION"/>. Only stages changes — it does
/// NOT call SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IAccountExceptionRepository
{
    Task AddAsync(TB_ACCOUNTEXCEPTION accountException, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_ACCOUNTEXCEPTION"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>). Returns <see langword="null"/> when no row with
    /// that <c>ID</c> exists — soft-deleted rows are still returned here.
    /// </summary>
    Task<TB_ACCOUNTEXCEPTION?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
