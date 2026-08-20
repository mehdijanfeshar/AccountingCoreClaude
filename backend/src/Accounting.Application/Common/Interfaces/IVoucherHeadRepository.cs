using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_VOUCHERSHEAD"/>. Only stages changes — it does NOT
/// call SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IVoucherHeadRepository
{
    Task AddAsync(TB_VOUCHERSHEAD voucherHead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_VOUCHERSHEAD"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_VOUCHERSHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes the entire detail subtree of the voucher head identified by
    /// <paramref name="headId"/> — BOTH levels below the head in one call:
    /// <list type="number">
    /// <item>every non-deleted <see cref="TB_VOUCHERSDETAIL"/> row belonging to the head, and</item>
    /// <item>every non-deleted <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI"/> row belonging to
    /// ANY <see cref="TB_VOUCHERSDETAIL"/> row belonging to the head (see the scoping note
    /// below — this is deliberately broader than "the detail rows this call just soft-deleted").</item>
    /// </list>
    /// Both levels are stamped with the same <paramref name="changeUserId"/>/
    /// <paramref name="updatedDate"/> the caller stamped on the head.
    ///
    /// Named "DetailTree" (not "DetailLines") and deliberately kept as ONE method rather than
    /// two, for three reasons:
    /// <list type="bullet">
    /// <item>The detail rows are already materialised here to compute which ones to soft-delete,
    /// so their IDs are already in memory — splitting level 3 into a second method would force a
    /// second query just to re-derive the same detail-ID set.</item>
    /// <item>Keeping both levels in one method makes it structurally impossible for a caller to
    /// cascade level 2 (details) without also cascading level 3 (tafsili links) — there is no
    /// separate entry point to forget to call.</item>
    /// <item>There is still no independent Command/Query for either <c>TB_VOUCHERSDETAIL</c> or
    /// <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> — both are only ever reachable as part of the voucher
    /// aggregate, so a dedicated per-table repository would be premature abstraction (same call
    /// already made for <c>ITokenManager</c>). If a standalone write path for either child table
    /// is ever introduced, this should be revisited/extracted.</item>
    /// </list>
    ///
    /// Implementations MUST load-and-mutate (never <c>ExecuteUpdateAsync</c>) for BOTH levels:
    /// the latter executes immediately against the database outside the EF Core change tracker,
    /// which would break the project's invariant that repositories only stage changes and the
    /// handler alone owns the transaction boundary via a single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> call. Without an explicit
    /// <c>BeginTransactionAsync</c> wrapper, an immediate child update could commit even if a
    /// sibling or the parent head update later fails — a partial cascade, unacceptable for
    /// accounting data. Row counts per voucher are bounded (tens of lines/links), so load+mutate
    /// is cheap.
    ///
    /// The detail-row filter is <c>ISDELETED IS NULL OR ISDELETED = FALSE</c> — i.e. it
    /// deliberately includes rows where <c>ISDELETED</c> is <see langword="null"/>, treating them
    /// as "not deleted" the same way the head handler already does. Note that a naive C#-style
    /// <c>d.ISDELETED != true</c> predicate does not reliably translate to SQL three-valued logic
    /// that keeps NULL rows, so the explicit <c>== null || == false</c> form must be used in the
    /// implementation. <see cref="TB_VOUCHERDETAIL_LINK_TAFSILI.ISDELETED"/> is a non-nullable
    /// <see cref="bool"/> (unlike the two upstream tables), so its filter is simply
    /// <c>== false</c> — no null branch needed.
    ///
    /// IMPORTANT scoping subtlety for level 3: the link query is NOT scoped to only the detail
    /// rows freshly soft-deleted by this call. It is scoped to EVERY <see cref="TB_VOUCHERSDETAIL"/>
    /// row under this head, regardless of that detail row's own <c>ISDELETED</c> state. Reason: if
    /// a detail row was previously soft-deleted on its own while its tafsili links stayed active,
    /// scoping the link query to only the newly-deleted details would leave those active links
    /// dangling under a now-deleted voucher. Deriving the link scope from "all details of the
    /// head" (not "details this call touched") is what guarantees the invariant "after a voucher
    /// is deleted, nothing beneath it remains active". This does mean the detail rows must be
    /// loaded unfiltered and then partitioned in memory (those needing soft-delete vs. the full
    /// ID set for the link query), rather than filtered directly in the SQL <c>Where</c> — but
    /// still via a single query against <c>TB_VOUCHERSDETAIL</c>.
    ///
    /// Returns the total number of rows staged for update across both levels (detail lines +
    /// tafsili links combined). Does NOT call <see cref="IUnitOfWork.SaveChangesAsync"/> — the
    /// caller (handler) does that exactly once after also stamping the head.
    /// </summary>
    Task<int> SoftDeleteDetailTreeAsync(
        Guid headId,
        string? changeUserId,
        DateTime updatedDate,
        CancellationToken cancellationToken = default);
}
