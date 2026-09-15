using Accounting.Application.AttribForAccountCodes.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_ATTRIBFORACCOUNTCODE</c>. Deliberately separate from
/// <see cref="IAttribForAccountCodeRepository"/> (the write-side repository) — this repository
/// never stages changes and always returns
/// <see cref="AttribForAccountCodes.Queries.AttribForAccountCodeDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IAttribForAccountCodeReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows belonging to <paramref name="vahedCode"/>, ordered by
    /// <c>VAHEDCODE</c>, then <c>YEAR</c>, then <c>ID</c> as a final tie-breaker for stable paging
    /// (the three columns together participate in <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c> alongside
    /// <c>ACCOUNTCODE_ID</c>, but no single column here is itself unique).
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
    Task<PagedResult<AttribForAccountCodeDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<AttribForAccountCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
