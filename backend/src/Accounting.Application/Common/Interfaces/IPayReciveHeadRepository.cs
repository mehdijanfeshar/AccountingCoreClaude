using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PAYRECIVHEAD"/> (Legacy payment/receipt document
/// header — سرسند دریافت و پرداخت). Only stages changes — it does NOT call SaveChanges; the
/// handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
///
/// ⚠️ <c>TB_PAYRECIVHEAD</c> is the aggregate root; <c>TB_PAYRECIVDETAIL</c> is not encapsulated
/// in the reference project either (<c>docs/tamin-core-entity-reference.md</c> §۵ — plain
/// <c>{ get; set; }</c>, unlike a permanently-embedded <c>*_LINK_TAFSIL*</c> table), but this
/// project still gives it no independent CRUD of its own: خزانه‌داری، بخش ۴-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰) is the first and only write path that ever creates
/// <c>TB_PAYRECIVDETAIL</c>/<c>TB_PAYRECIVDETAIL_LINK_TAFSILI</c> rows, and it always does so
/// alongside a brand-new <c>TB_PAYRECIVHEAD</c> row in the very same
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call — so the two detail-write methods below live
/// on the parent's own repository, parent-scoped, the same shape
/// <c>IVoucherDetailRepository.AddTafsiliLinkAsync</c> uses for a genuinely embedded table. No
/// standalone detail Update/Delete exists (out of scope — nothing in بخش ۴-ب ever edits a written
/// PayReciv detail line).
/// </summary>
public interface IPayReciveHeadRepository
{
    Task AddAsync(TB_PAYRECIVHEAD payReciveHead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PAYRECIVHEAD"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_PAYRECIVHEAD?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// بخش ۴-ب — next value of the per-(VAHEDCODE, YEAR) counter behind <c>PAYRECIVCODE</c>.
    /// Unlike <c>IPaymentRequestRepository.GetNextCodeAsync</c>'s <c>"PAY-" + 6 digits</c>, this
    /// column is only <c>VARCHAR2(5)</c> (no room for any prefix) — plain zero-padded 5-digit
    /// string, e.g. <c>"00001"</c>. Same client-side-parsed-MAX approach (deleted rows counted on
    /// purpose — no UNIQUE constraint exists on this column at all, so nothing actually enforces
    /// non-reuse, but a soft-deleted row's code should still never be handed out again by this
    /// generator specifically).
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <see cref="TB_PAYRECIVDETAIL"/> row for insert, parent-scoped to the
    /// <see cref="TB_PAYRECIVHEAD"/> it belongs to (see this interface's class XML doc for why
    /// this lives here rather than on a standalone detail repository).
    /// </summary>
    Task AddDetailAsync(TB_PAYRECIVDETAIL payReciveDetail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <see cref="TB_PAYRECIVDETAIL_LINK_TAFSILI"/> row for insert, parent-scoped to
    /// its <see cref="TB_PAYRECIVDETAIL"/> line.
    /// </summary>
    Task AddDetailTafsiliLinkAsync(TB_PAYRECIVDETAIL_LINK_TAFSILI tafsiliLink, CancellationToken cancellationToken = default);
}
