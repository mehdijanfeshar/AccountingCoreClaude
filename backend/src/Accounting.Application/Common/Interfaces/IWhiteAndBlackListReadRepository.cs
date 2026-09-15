using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_WHITEANDBLACKLIST</c>. Deliberately separate from
/// <see cref="IWhiteAndBlackListRepository"/> (the write-side repository) — this repository
/// never stages changes and always returns
/// <see cref="WhiteAndBlackLists.Queries.WhiteAndBlackListDto"/> projections, never the Domain
/// entity.
/// </summary>
public interface IWhiteAndBlackListReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted white/black-list rows ordered by <c>CREATEDDATE</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<WhiteAndBlackListDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the white/black-list row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<WhiteAndBlackListDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
