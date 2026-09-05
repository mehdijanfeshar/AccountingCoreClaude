using Accounting.Application.Expenses.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_EXPENCE</c>. Deliberately separate from
/// <see cref="IExpenseRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="Expenses.Queries.ExpenseDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IExpenseReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted expense rows ordered by <c>EXPENCECODE</c>, then <c>ID</c>
    /// as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<ExpenseDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the expense row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
