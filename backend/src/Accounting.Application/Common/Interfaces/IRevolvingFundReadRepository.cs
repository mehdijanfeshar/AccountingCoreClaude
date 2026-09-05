using Accounting.Application.RevolvingFunds.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_REVOLVING_FUND</c>. Deliberately separate from
/// <see cref="IRevolvingFundRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="RevolvingFunds.Queries.RevolvingFundDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IRevolvingFundReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted revolving-fund rows ordered by <c>CODE</c>, then <c>ID</c>
    /// as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<RevolvingFundDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the revolving-fund row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<RevolvingFundDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
