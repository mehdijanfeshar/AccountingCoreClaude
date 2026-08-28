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
    /// Returns a page of non-deleted rows ordered by <c>VAHEDCODE</c>, then <c>YEAR</c>, then
    /// <c>ID</c> as a final tie-breaker for stable paging (the three columns together
    /// participate in <c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c> alongside <c>ACCOUNTCODE_ID</c>, but
    /// no single column here is itself unique).
    /// </summary>
    Task<PagedResult<AttribForAccountCodeDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<AttribForAccountCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
