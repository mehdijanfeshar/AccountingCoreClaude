using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_WHITEANDBLACKLIST"/>. Only stages changes — it does
/// NOT call SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IWhiteAndBlackListRepository
{
    Task AddAsync(TB_WHITEANDBLACKLIST whiteAndBlackList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_WHITEANDBLACKLIST"/> by <c>ID</c> as a change-tracked
    /// entity (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_WHITEANDBLACKLIST?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
