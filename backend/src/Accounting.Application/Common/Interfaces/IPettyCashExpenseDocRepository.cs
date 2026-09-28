using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_EXPENSE_DOC"/> ("صورت‌هزینهٔ تنخواه"). Only stages
/// changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashExpenseDocRepository
{
    Task AddAsync(TB_PC_EXPENSE_DOC expenseDoc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_EXPENSE_DOC"/> by <c>ID</c> as change-tracked, verifying
    /// unit ownership via <c>VahedOwnership</c> like every other by-id repository method in this
    /// project. Returns <see langword="null"/> when no row with that <c>ID</c> exists —
    /// soft-deleted rows are still returned (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_PC_EXPENSE_DOC?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a non-deleted, non-<see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Rejected"/>
    /// row for <paramref name="vahedCode"/> already carries the same
    /// (<paramref name="vendorNationalId"/>, <paramref name="invoiceNo"/>) pair, excluding
    /// <paramref name="excludeId"/> itself (used by Update, so a document does not collide with
    /// its own unchanged values). See <c>docs/tankhah-khazaneh-module.md</c> §4.
    /// </summary>
    Task<bool> ExistsActiveDuplicateAsync(
        string vendorNationalId,
        string invoiceNo,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The §2 balance-equation inputs for one fund: total amount and count of non-deleted
    /// documents currently <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/>,
    /// and separately of those New/PendingReview/Returned ("in flight"). Always excludes
    /// <paramref name="excludeDocId"/> when supplied, so a Submit check can ask "if I add THIS
    /// document's own amount, does it still fit" regardless of whether the document was
    /// previously Draft (not yet counted at all) or Returned (already counted as in-flight).
    /// </summary>
    Task<PettyCashFundExposure> GetFundExposureAsync(
        Guid fundId,
        string vahedCode,
        Guid? excludeDocId,
        CancellationToken cancellationToken = default);
}
