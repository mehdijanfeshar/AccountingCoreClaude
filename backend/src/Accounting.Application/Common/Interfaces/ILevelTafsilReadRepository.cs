using Accounting.Application.Common;
using Accounting.Application.LevelTafsils.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_LEVEL_TAFSIL</c>. Deliberately separate from
/// <see cref="ILevelTafsilRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="LevelTafsils.Queries.LevelTafsilDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface ILevelTafsilReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows ordered by <c>LEVEL_CODE</c>, then <c>ID</c> as a
    /// tie-breaker for stable paging. Neither column carries a UNIQUE constraint on this table.
    /// </summary>
    Task<PagedResult<LevelTafsilDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<LevelTafsilDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
