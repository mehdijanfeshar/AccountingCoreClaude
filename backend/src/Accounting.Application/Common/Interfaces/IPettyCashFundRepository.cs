using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_FUND"/> ("تنخواه") — the module's own, fully
/// independent تنخواه table (2026-09-28 decision, <c>docs/tankhah-khazaneh-module.md</c> §0),
/// replacing <c>IRevolvingFundRepository</c> for this module entirely. Only stages changes — never
/// calls SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashFundRepository
{
    Task AddAsync(TB_PC_FUND fund, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_FUND"/> by <c>ID</c> as a change-tracked entity so
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core generate
    /// the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are still
    /// returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_PC_FUND?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a non-deleted <c>TB_PC_FUND</c> row for <paramref name="vahedCode"/> already
    /// carries <paramref name="code"/>, excluding <paramref name="excludeId"/> itself (used by
    /// Update, so a fund does not collide with its own unchanged code). Backs the 409 duplicate-code
    /// check in Create/Update — <c>UK_PC_FUND_CODE</c> would also catch this at the DB level, but
    /// this application-level check gives a clean, attributable error instead of a generic
    /// <c>DuplicateKeyException</c>.
    /// </summary>
    Task<bool> ExistsByCodeAsync(
        string code,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one non-deleted <c>TB_PC_EXPENSE_DOC</c> row still references
    /// <paramref name="fundId"/>. Backs the delete guard — a تنخواه with live صورت‌هزینه rows may
    /// not be deleted (409), per this batch's task description.
    /// </summary>
    Task<bool> HasActiveExpenseDocsAsync(Guid fundId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <see cref="TB_PC_FUND_LINK_TAFSILI"/> row for insert. Only stages — the
    /// handler still owns the single <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Deliberately NOT named <c>AddAsync</c>: same reasoning as
    /// <see cref="IExpenseRepository.AddTafsiliLinkAsync"/> — <c>TB_PC_FUND_LINK_TAFSILI</c> is a
    /// permanently-embedded link table (team rule, <c>docs/tamin-core-entity-reference.md</c>
    /// section 5; <c>NoIndependentLinkTableWritePathTests</c>) mutated only via this
    /// explicitly-named, parent-scoped method on the PARENT aggregate's own repository, never via
    /// an aggregate-root-shaped <c>AddAsync</c>/<c>GetForUpdateAsync</c> pair.
    /// </summary>
    Task AddFundTafsiliLinkAsync(TB_PC_FUND_LINK_TAFSILI link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the fund's currently-active (<c>ISDELETED == false</c>)
    /// <see cref="TB_PC_FUND_LINK_TAFSILI"/> rows, change-tracked, so the upsert handler can
    /// reconcile the caller's requested replacement set against them. Returns an empty list —
    /// never <see langword="null"/> — when the fund has none yet.
    /// </summary>
    Task<IReadOnlyList<TB_PC_FUND_LINK_TAFSILI>> GetActiveFundTafsiliLinksAsync(
        Guid fundId, CancellationToken cancellationToken = default);
}
