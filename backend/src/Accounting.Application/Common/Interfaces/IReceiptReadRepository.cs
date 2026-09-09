using Accounting.Application.Receipts.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_RECEIP</c>. Deliberately separate from
/// <see cref="IReceiptRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="Receipts.Queries.ReceiptDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IReceiptReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted receipt rows belonging to <paramref name="vahedCode"/>,
    /// ordered by <c>RECEIPT_NO</c>, then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="vahedCode">
    /// Organizational unit code to filter by — required, not nullable. Rows are matched with
    /// exact equality only (<c>VAHEDCODE == vahedCode</c>); rows with <c>VAHEDCODE IS NULL</c>
    /// are never returned to anyone, by deliberate fail-closed design (see implementation XML
    /// doc).
    /// </param>
    /// <param name="cancellationToken">Propagated to the underlying EF Core query.</param>
    Task<PagedResult<ReceiptDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the receipt row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<ReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
