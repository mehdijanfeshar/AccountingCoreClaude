using Accounting.Application.BillLogs.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_BILL_LOG</c>. Deliberately separate from
/// <see cref="IBillLogRepository"/> — never stages changes and always returns
/// <see cref="BillLogDto"/> projections, never the Domain entity.
/// </summary>
public interface IBillLogReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows ordered by <c>CREATEDDATE</c>, then <c>ID</c> as a
    /// tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<BillLogDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<BillLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
