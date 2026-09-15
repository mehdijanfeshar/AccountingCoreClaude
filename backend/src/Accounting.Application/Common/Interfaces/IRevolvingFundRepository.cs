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

    /// <summary>
    /// Stages a new <see cref="TB_REVOLVINGFUND_LINK_TAFSILI"/> row for insert as part of its parent
    /// revolving fund's write. Only stages — the handler still owns the single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, so the revolving fund and its تفصیلی links are
    /// always persisted atomically.
    ///
    /// <b>Deliberately named <c>AddTafsiliLinkAsync</c>, not <c>AddAsync</c>.</b> Same reasoning as
    /// <see cref="IBankAccountRepository.AddTafsiliLinkAsync"/>: an aggregate-root-shaped
    /// <c>AddAsync</c> taking a <c>*_LINK_TAFSIL*</c> entity would promote this permanently embedded
    /// table to an aggregate root of its own, which <c>NoIndependentLinkTableWritePathTests</c>
    /// forbids.
    /// </summary>
    Task AddTafsiliLinkAsync(TB_REVOLVINGFUND_LINK_TAFSILI link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the revolving fund's currently-active (<c>ISDELETED == false</c>) تفصیلی links as
    /// change-tracked entities, so the update handler can reconcile the caller's requested set
    /// against them and soft-delete the dropped ones in place, inside its single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Returns an empty list — never <see langword="null"/> — when there are no links.
    /// </summary>
    Task<IReadOnlyList<TB_REVOLVINGFUND_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid revolvingFundId,
        CancellationToken cancellationToken = default);
}
