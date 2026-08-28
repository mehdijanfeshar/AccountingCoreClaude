using Accounting.Application.Common;
using Accounting.Application.TafsilGroups.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_TAFSIL_GROUP</c>. Deliberately separate from
/// <see cref="ITafsilGroupRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="TafsilGroups.Queries.TafsilGroupDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface ITafsilGroupReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows ordered by <c>TAFSILGROUP_CODE</c> (which participates
    /// in <c>UK_TBTAFSILGROUP</c> alongside <c>ISDELETED</c>, but is not itself unique on its
    /// own), then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<TafsilGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<TafsilGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
