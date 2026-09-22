using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <c>TB_IDENTITYHEAD</c> (شناسنامه) and its embedded
/// <c>TB_IDENTITYFIXITEMS</c> rows.
///
/// <b>The fix items deliberately have no repository, controller or MediatR request of their
/// own.</b> They are part of this aggregate: the reference project builds them inside
/// <c>IdentityHead.AddIdentityFixItems(...)</c> on the head entity itself, and a fix item is
/// meaningless without its head. This follows the sanctioned embedded-table shape that
/// <c>NoIndependentLinkTableWritePathTests</c> enforces — parent-scoped methods hanging off the
/// parent's own repository, exactly like <see cref="IBankAccountRepository.AddTafsiliLinkAsync"/>.
///
/// ⚠️ <c>TB_IDENTITYDETAIL</c> is NOT part of this aggregate and is not touched here. Those rows
/// carry the <b>variable</b> subgroup values and hang off a <c>TB_VOUCHERSDETAIL</c> row, so they
/// belong to voucher entry, not to شناسنامه base data. See <c>IdentityHeadsController</c>.
/// </summary>
public interface IIdentityHeadRepository
{
    Task AddAsync(TB_IDENTITYHEAD identityHead, CancellationToken cancellationToken = default);

    Task<TB_IDENTITYHEAD?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Deliberately named <c>AddFixItemAsync</c>, not <c>AddAsync</c>.</b> Same reasoning as
    /// <see cref="IBankAccountRepository.AddTafsiliLinkAsync"/>: an aggregate-root-shaped name
    /// keeps it obvious at every call site that this is not an independent write path.
    /// </summary>
    Task AddFixItemAsync(TB_IDENTITYFIXITEM fixItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the non-deleted fix items of one head, tracked so the caller can mutate them
    /// in place (the update path rewrites values rather than deleting and re-inserting, so an
    /// untouched item keeps its original audit trail).
    /// </summary>
    Task<IReadOnlyList<TB_IDENTITYFIXITEM>> GetActiveFixItemsAsync(
        Guid identityHeadId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Next <c>SERIAL</c> for the given group within the caller's unit and year — one past the
    /// highest currently stored, starting at 1.
    ///
    /// ⚠️ <b>This is deliberately not race-proof, and does not need to be.</b> Two concurrent
    /// creates can compute the same serial, but <c>AK_AK_IDENTYHEAD_IDENTYHE</c> is a real UNIQUE
    /// constraint on <c>(IDENTITYGROUPS_ID, SERIAL, VAHEDCODE, YEAR)</c>, so the loser gets
    /// ORA-00001, which <c>UnitOfWork.SaveChangesAsync</c> already maps centrally to a 409. That
    /// is the project's established answer to this shape of race (see
    /// <c>AttribForAccountCodesController</c>): let the DB constraint be the source of truth
    /// rather than adding a pre-check that is itself racy.
    /// </summary>
    Task<int> GetNextSerialAsync(
        Guid identityGroupId,
        string vahedCode,
        string year,
        CancellationToken cancellationToken = default);
}
