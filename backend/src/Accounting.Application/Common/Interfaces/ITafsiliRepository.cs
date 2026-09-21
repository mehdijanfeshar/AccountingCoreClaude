using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TAFSILI"/>. Only stages changes — it does NOT call
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface ITafsiliRepository
{
    Task AddAsync(TB_TAFSILI tafsili, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_TAFSILI"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>) so that Update/Delete handlers can mutate the
    /// returned instance in place and have EF Core generate the correct UPDATE on
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns <see langword="null"/> when no row
    /// with that <c>ID</c> exists — soft-deleted rows are still returned here (the caller
    /// decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_TAFSILI?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every non-soft-deleted <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row belonging to
    /// <paramref name="tafsiliId"/>, change-tracked (no <c>AsNoTracking()</c>).
    ///
    /// <b>Deliberately named <c>GetTafsiliGroupLinksAsync</c>, not <c>GetForUpdateAsync</c>.</b>
    /// <c>TB_TAFSIL_LINK_TAFSILGROUP</c> is one of the project's permanently-embedded
    /// <c>*_LINK_TAFSIL*</c> tables (<c>docs/open-decisions.md</c>) — it must never get an
    /// aggregate-root-shaped write path of its own. This method exists only so
    /// <c>SetTafsilGroupLinksAsync</c> can reconcile the set in place, exactly the same shape as
    /// the pre-existing <c>IVoucherDetailRepository.GetActiveTafsiliLinksAsync</c> /
    /// <c>AddTafsiliLinkAsync</c> precedent for <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>. There is
    /// deliberately no <c>ITafsilLinkTafsilGroupRepository</c>, no controller, and no MediatR
    /// request for this table.
    /// </summary>
    Task<IReadOnlyList<TB_TAFSIL_LINK_TAFSILGROUP>> GetTafsiliGroupLinksAsync(
        Guid tafsiliId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <c>TB_TAFSIL_LINK_TAFSILGROUP</c> row. See
    /// <see cref="GetTafsiliGroupLinksAsync"/> XML doc for why this is parent-scoped rather than
    /// an independent write path.
    /// </summary>
    Task AddTafsiliGroupLinkAsync(TB_TAFSIL_LINK_TAFSILGROUP link, CancellationToken cancellationToken = default);
}
