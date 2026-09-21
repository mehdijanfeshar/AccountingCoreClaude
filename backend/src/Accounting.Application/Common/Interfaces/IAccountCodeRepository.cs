using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_ACCOUNTCODE"/>. Only stages changes — it does NOT
/// call SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IAccountCodeRepository
{
    Task AddAsync(TB_ACCOUNTCODE accountCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_ACCOUNTCODE"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_ACCOUNTCODE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_ACCOUNT_LINK_TAFSILGROUP"/> row scoped to
    /// <paramref name="accountCodeId"/>, as a change-tracked entity — <see langword="null"/> if
    /// no row with that <paramref name="linkId"/> exists under that معین. Scoped by the PARENT's
    /// id so a caller can never address a link row directly by its own id alone.
    ///
    /// <b>Deliberately named <c>GetTafsilGroupLinkForUpdateAsync</c>, not <c>GetForUpdateAsync</c>.</b>
    /// <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> is one of the project's permanently-embedded
    /// <c>*_LINK_TAFSIL*</c> tables (<c>docs/open-decisions.md</c>) — <c>NoIndependentLinkTableWritePathTests</c>
    /// asserts no method literally named <c>GetForUpdateAsync</c> ever returns such an entity. A
    /// parent-scoped, explicitly-named method on the PARENT aggregate's own repository is the
    /// sanctioned way to mutate an embedded table — mirrors
    /// <c>IVoucherDetailRepository.AddTafsiliLinkAsync</c>/<c>GetActiveTafsiliLinksAsync</c>
    /// (phase 11) for <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c>.
    /// </summary>
    Task<TB_ACCOUNT_LINK_TAFSILGROUP?> GetTafsilGroupLinkForUpdateAsync(
        Guid accountCodeId,
        Guid linkId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> row. See
    /// <see cref="GetTafsilGroupLinkForUpdateAsync"/> XML doc for why this is parent-scoped
    /// rather than an independent write path.
    /// </summary>
    Task AddTafsilGroupLinkAsync(TB_ACCOUNT_LINK_TAFSILGROUP link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every <see cref="TB_ACCOUNT_LINK_TAFSILGROUP"/> row this معین <b>will have</b> once the
    /// current unit of work is committed — soft-deleted rows included, as change-tracked entities.
    ///
    /// <b>"Will have", not "has", and that is the point.</b> A row already staged for insert in
    /// this same unit of work (see <see cref="AddTafsilGroupLinkAsync"/>) is not in the database
    /// yet and a plain query would not return it, so the implementation merges pending inserts in.
    /// Without that, <c>AccountLevelLinkSynchronizer</c> would reconcile against the state from
    /// before the caller's own change and immediately undo it.
    ///
    /// Rows are change-tracked on purpose: a caller that has already mutated one (an update or an
    /// unlink) gets back that same instance through EF's identity map, so the set it computes
    /// reflects the change it just made rather than the row as stored.
    /// </summary>
    Task<IReadOnlyList<TB_ACCOUNT_LINK_TAFSILGROUP>> GetTafsilGroupLinksForSyncAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every <see cref="TB_ACCOUNT_LINK_LEVEL"/> row of this معین — soft-deleted ones included,
    /// change-tracked. The deleted rows matter: the synchronizer revives one rather than inserting
    /// a second row for the same level, so a level switched off and back on keeps a single row
    /// with its original <c>CREATEDDATE</c>.
    /// </summary>
    Task<IReadOnlyList<TB_ACCOUNT_LINK_LEVEL>> GetLevelLinksForSyncAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages new <see cref="TB_ACCOUNT_LINK_LEVEL"/> rows. Parent-scoped like the گروه تفصیلی
    /// links above: <c>TB_ACCOUNT_LINK_LEVEL</c> has no write path of its own and is never
    /// addressed except through the معین that owns it.
    /// </summary>
    Task AddLevelLinksAsync(
        IReadOnlyCollection<TB_ACCOUNT_LINK_LEVEL> links,
        CancellationToken cancellationToken = default);
}
