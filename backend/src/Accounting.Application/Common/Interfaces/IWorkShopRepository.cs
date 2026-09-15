using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_WORKSHOP"/>. Only stages changes — it does NOT call
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IWorkShopRepository
{
    Task AddAsync(TB_WORKSHOP workShop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_WORKSHOP"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_WORKSHOP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <see cref="TB_WORKSHOP_LINK_TAFSILI"/> row for insert as part of its parent
    /// workshop's write. Only stages — the handler still owns the single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, so the workshop and its تفصیلی links are
    /// always persisted atomically.
    ///
    /// <b>Deliberately named <c>AddTafsiliLinkAsync</c>, not <c>AddAsync</c>.</b> Same reasoning as
    /// <see cref="IBankAccountRepository.AddTafsiliLinkAsync"/>: an aggregate-root-shaped
    /// <c>AddAsync</c> taking a <c>*_LINK_TAFSIL*</c> entity would promote this permanently embedded
    /// table to an aggregate root of its own, which <c>NoIndependentLinkTableWritePathTests</c>
    /// forbids.
    /// </summary>
    Task AddTafsiliLinkAsync(TB_WORKSHOP_LINK_TAFSILI link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workshop's currently-active (<c>ISDELETED == false</c>) تفصیلی links as
    /// change-tracked entities, so the update handler can reconcile the caller's requested set
    /// against them and soft-delete the dropped ones in place, inside its single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Returns an empty list — never <see langword="null"/> — when there are no links.
    /// </summary>
    Task<IReadOnlyList<TB_WORKSHOP_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid workShopId,
        CancellationToken cancellationToken = default);
}
