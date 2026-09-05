using Accounting.Application.BankCartDetails.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_BANKCARTDETAIL</c>. Deliberately separate from
/// <see cref="IBankCartDetailRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="BankCartDetails.Queries.BankCartDetailDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IBankCartDetailReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows ordered by <c>YEAR</c>, then <c>MONTH</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging. Both <c>YEAR</c> and <c>MONTH</c> are
    /// nullable, so Oracle sorts NULLs last.
    /// </summary>
    Task<PagedResult<BankCartDetailDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<BankCartDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
