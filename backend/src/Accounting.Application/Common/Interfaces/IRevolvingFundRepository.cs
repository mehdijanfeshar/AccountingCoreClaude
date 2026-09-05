using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_REVOLVING_FUND"/> (Legacy revolving fund / تنخواه
/// master). Only stages changes — it does NOT call SaveChanges; the handler owns the transaction
/// boundary via <see cref="IUnitOfWork"/>.
///
/// ⚠️ This table's child <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> is permanently embedded per the
/// team rule that every <c>*_LINK_TAFSIL*</c> table never gets an independent write path — this
/// repository deliberately has no method touching it.
/// </summary>
public interface IRevolvingFundRepository
{
    Task AddAsync(TB_REVOLVING_FUND revolvingFund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_REVOLVING_FUND"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_REVOLVING_FUND?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
