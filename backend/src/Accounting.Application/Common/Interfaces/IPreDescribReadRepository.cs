using Accounting.Application.PreDescribs.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_PREDESCRIB</c>. Deliberately separate from
/// <see cref="IPreDescribRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="PreDescribs.Queries.PreDescribDto"/>
/// projections, never the Domain entity.
///
/// Unlike every other read repository in this project, <see cref="GetPagedAsync"/> applies no
/// <c>ISDELETED</c> filter — <c>TB_PREDESCRIB</c> has no such column, and
/// <see cref="PreDescribs.Queries.PreDescribDto"/> has no <c>IsDeleted</c> field to expose one.
/// It DOES apply the standard <c>VAHEDCODE</c> unit-scope filter, same as every other read
/// repository in this project.
/// </summary>
public interface IPreDescribReadRepository
{
    /// <summary>
    /// Returns a page of pre-description rows belonging to <paramref name="vahedCode"/> (no
    /// logical-delete filter — the column does not exist), ordered by <c>DESCRIP</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="vahedCode">
    /// Organizational unit code to filter by — required, not nullable. Rows are matched with
    /// exact equality only (<c>VAHEDCODE == vahedCode</c>); rows with <c>VAHEDCODE IS NULL</c>
    /// are never returned to anyone, by deliberate fail-closed design (see implementation XML
    /// doc).
    /// </param>
    /// <param name="cancellationToken">Propagated to the underlying EF Core query.</param>
    Task<PagedResult<PreDescribDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the pre-description row with the given <paramref name="id"/>, or
    /// <see langword="null"/> if no such row exists.
    /// </summary>
    Task<PreDescribDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
