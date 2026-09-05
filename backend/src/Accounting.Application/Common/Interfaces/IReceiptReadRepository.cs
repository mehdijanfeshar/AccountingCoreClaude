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
    /// Returns a page of non-deleted receipt rows ordered by <c>RECEIPT_NO</c>, then <c>ID</c> as
    /// a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<ReceiptDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the receipt row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<ReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
