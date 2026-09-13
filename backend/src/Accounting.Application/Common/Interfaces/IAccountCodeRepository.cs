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
}
