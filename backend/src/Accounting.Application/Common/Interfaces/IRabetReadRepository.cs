using Accounting.Application.Rabets.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_RABET</c>. Deliberately separate from
/// <see cref="IRabetRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="Rabets.Queries.RabetDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IRabetReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rabet rows ordered by <c>CREATEDDATE</c>, then <c>ID</c>
    /// as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<RabetDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the rabet row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<RabetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
