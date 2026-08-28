using Accounting.Application.WorkShops.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_WORKSHOP</c>. Deliberately separate from
/// <see cref="IWorkShopRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="WorkShops.Queries.WorkShopDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IWorkShopReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted workshop rows ordered by <c>WORKSHOPCODE</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<WorkShopDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workshop row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<WorkShopDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
