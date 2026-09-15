using Accounting.Application.VahedInfos.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_VAHED_INFO</c>. Deliberately separate from
/// <see cref="IVahedInfoRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="VahedInfos.Queries.VahedInfoDto"/>
/// projections, never the Domain entity.
///
/// Neither query here applies (or could apply) an <c>ISDELETED</c> filter — <c>TB_VAHED_INFO</c>
/// has no such column, and <see cref="VahedInfos.Queries.VahedInfoDto"/> has no
/// <c>IsDeleted</c> field to expose one.
/// </summary>
public interface IVahedInfoReadRepository
{
    /// <summary>
    /// Returns a page of all organizational-unit rows (no logical-delete filter — the column
    /// does not exist), ordered by <c>VAHEDCODE</c>, then <c>ID</c> as a tie-breaker for stable
    /// paging.
    /// </summary>
    Task<PagedResult<VahedInfoDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the organizational-unit row with the given <paramref name="id"/>, or
    /// <see langword="null"/> if no such row exists.
    /// </summary>
    Task<VahedInfoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
