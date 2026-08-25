using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_VOUCHERSDETAIL"/>. Only stages changes — it does NOT
/// call SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
///
/// <c>TB_VOUCHERSDETAIL</c> is an independent aggregate root with its own standalone CRUD
/// (2026-08-20 project-owner decision, recorded in <c>docs/tamin-core-entity-reference.md</c>
/// بخش ۵) — it is used both by <see cref="Accounting.Application.Vouchers.Commands.CreateVoucherHead.CreateVoucherHeadCommandHandler"/>
/// (composite create alongside a brand-new head) and by the standalone
/// <c>CreateVoucherDetailCommand</c>/<c>UpdateVoucherDetailCommand</c>/<c>DeleteVoucherDetailCommand</c>
/// handlers (add/edit/remove a line on an already-existing voucher). The phase-9 cascade
/// soft-delete of a voucher head's full detail tree remains entirely owned by
/// <see cref="IVoucherHeadRepository.SoftDeleteDetailTreeAsync"/> and is untouched by this
/// interface — see that method's XML doc for why it was intentionally left in place rather than
/// refactored to route through this repository.
/// </summary>
public interface IVoucherDetailRepository
{
    Task AddAsync(TB_VOUCHERSDETAIL voucherDetail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_VOUCHERSDETAIL"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>), mirroring
    /// <see cref="IVoucherHeadRepository.GetForUpdateAsync"/>.
    /// </summary>
    Task<TB_VOUCHERSDETAIL?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes every non-deleted <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI"/> row that
    /// belongs to the given <paramref name="detailId"/>, stamping each with
    /// <paramref name="changeUserId"/>/<paramref name="updatedDate"/>. Used by
    /// <c>DeleteVoucherDetailCommandHandler</c> so a standalone detail-line delete cascades to
    /// its own tafsili links in the SAME <see cref="IUnitOfWork.SaveChangesAsync"/> call —
    /// applying the phase-9 invariant "after a soft-delete, nothing beneath it remains active"
    /// one level down, consistent with the team rule that every <c>*_LINK_TAFSIL*</c> table is
    /// always embedded and never independently writable (never invented here — see
    /// <c>docs/tamin-core-entity-reference.md</c> بخش ۵).
    ///
    /// The filter is <c>ISDELETED == false</c> only — unlike the two-level filter used by
    /// <see cref="IVoucherHeadRepository.SoftDeleteDetailTreeAsync"/> for the same table,
    /// because <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI.ISDELETED"/> is a non-nullable
    /// <see cref="bool"/> here too (verified against the Fluent mapping), so there is no NULL
    /// branch to account for.
    ///
    /// Implementation MUST load-and-mutate (never <c>ExecuteUpdateAsync</c>) — same rationale as
    /// <see cref="IVoucherHeadRepository.SoftDeleteDetailTreeAsync"/>: an immediate DB-side
    /// update would execute outside the EF Core change tracker and could commit even if the
    /// detail row's own soft-delete later fails, producing a partial cascade.
    ///
    /// Returns the number of link rows staged for update. Does NOT call
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> — the caller (handler) does that exactly once
    /// after also stamping the detail row itself.
    /// </summary>
    Task<int> SoftDeleteTafsiliLinksAsync(
        Guid detailId,
        string? changeUserId,
        DateTime updatedDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI"/> row for insert as part of its
    /// parent detail line's write. Only stages — the handler still owns the single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, so a detail line and its tafsili links are
    /// always persisted atomically.
    ///
    /// <b>Deliberately named <c>AddTafsiliLinkAsync</c>, not <c>AddAsync</c>.</b> The name is not
    /// cosmetic: <c>AddAsync</c>/<c>GetForUpdateAsync</c> are this project's aggregate-root-shaped
    /// method names, and <c>NoIndependentLinkTableWritePathTests</c> asserts no method of that
    /// shape ever accepts or returns a <c>*_LINK_TAFSIL*</c>/<c>*_LINK_LEVEL*</c> entity. A
    /// parent-scoped, explicitly-named method on the PARENT aggregate's repository is the
    /// sanctioned way to mutate an embedded table — the same shape as the pre-existing
    /// <see cref="SoftDeleteTafsiliLinksAsync"/>. There is deliberately no
    /// <c>ITafsiliLinkRepository</c>, no controller, and no MediatR request for this table.
    /// </summary>
    Task AddTafsiliLinkAsync(
        TB_VOUCHERDETAIL_LINK_TAFSILI tafsiliLink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads every non-soft-deleted <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI"/> row belonging to
    /// <paramref name="detailId"/> as change-tracked entities (deliberately no
    /// <c>AsNoTracking()</c>), so that <c>UpdateVoucherDetailCommandHandler</c> can reconcile the
    /// caller's requested set against them and soft-delete the ones that were dropped, in place,
    /// within the handler's single <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Filter is a plain <c>ISDELETED == false</c> — <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI.ISDELETED"/>
    /// is a non-nullable <see cref="bool"/>, so unlike the two upstream voucher tables there is no
    /// NULL branch to account for (same note as on <see cref="SoftDeleteTafsiliLinksAsync"/>).
    ///
    /// Returns an empty list — never <see langword="null"/> — when the line has no active links.
    /// </summary>
    Task<IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid detailId,
        CancellationToken cancellationToken = default);
}
