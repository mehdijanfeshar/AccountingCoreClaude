using Accounting.Application.WhiteLists.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_WHITELIST</c>. Deliberately separate from
/// <see cref="IWhiteListRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="WhiteLists.Queries.WhiteListDto"/> projections,
/// never the Domain entity.
/// </summary>
public interface IWhiteListReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted white-list rows ordered by <c>CREATEDDATE</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<WhiteListDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the white-list row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<WhiteListDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
