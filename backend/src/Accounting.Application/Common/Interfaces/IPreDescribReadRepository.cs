using Accounting.Application.PreDescribs.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_PREDESCRIB</c>. Deliberately separate from
/// <see cref="IPreDescribRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="PreDescribs.Queries.PreDescribDto"/>
/// projections, never the Domain entity.
///
/// Unlike every other read repository in this project, neither query here applies (or could
/// apply) an <c>ISDELETED</c> filter — <c>TB_PREDESCRIB</c> has no such column, and
/// <see cref="PreDescribs.Queries.PreDescribDto"/> has no <c>IsDeleted</c> field to expose one.
/// </summary>
public interface IPreDescribReadRepository
{
    /// <summary>
    /// Returns a page of all pre-description rows (no logical-delete filter — the column does
    /// not exist), ordered by <c>DESCRIP</c>, then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<PreDescribDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the pre-description row with the given <paramref name="id"/>, or
    /// <see langword="null"/> if no such row exists.
    /// </summary>
    Task<PreDescribDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
