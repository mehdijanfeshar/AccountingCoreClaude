using Accounting.Application.ElamHeads.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_ELAMHEAD</c>. Deliberately separate from
/// <see cref="IElamHeadRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="ElamHeads.Queries.ElamHeadDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IElamHeadReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted announcement-header rows ordered by
    /// <c>ELAMH_SERIALNO</c> (nullable — Oracle sorts NULLs last), then <c>ID</c> as a
    /// tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<ElamHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the announcement-header row with the given <paramref name="id"/> regardless of
    /// its logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<ElamHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
