using Accounting.Application.ChequeTypes.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_CHECK_TYPE</c>. Deliberately separate from
/// <see cref="IChequeTypeRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="ChequeTypes.Queries.ChequeTypeDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IChequeTypeReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted cheque-type rows ordered by <c>CREATEDDATE</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<ChequeTypeDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cheque-type row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<ChequeTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
