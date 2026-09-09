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
    /// Returns a page of non-deleted rows belonging to <paramref name="vahedCode"/>, ordered by
    /// <c>YEAR</c>, then <c>MONTH</c>, then <c>ID</c> as a tie-breaker for stable paging. Both
    /// <c>YEAR</c> and <c>MONTH</c> are nullable, so Oracle sorts NULLs last.
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
    Task<PagedResult<BankCartDetailDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<BankCartDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
