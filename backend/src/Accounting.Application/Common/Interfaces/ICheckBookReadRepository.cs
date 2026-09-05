using Accounting.Application.CheckBooks.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_CHECKBOOK</c>. Deliberately separate from
/// <see cref="ICheckBookRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="CheckBooks.Queries.CheckBookDto"/> projections,
/// never the Domain entity.
/// </summary>
public interface ICheckBookReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted checkbook rows ordered by <c>FROMCHECKNUMBER</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<CheckBookDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the checkbook row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<CheckBookDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
