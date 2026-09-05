using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.Expenses.Queries.GetExpenses;

/// <summary>
/// Returns a page of <c>TB_EXPENCE</c> rows projected to <see cref="ExpenseDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetExpensesQueryValidator.MaxPageSize"/>.</param>
public sealed record GetExpensesQuery(int PageNumber, int PageSize) : IRequest<PagedResult<ExpenseDto>>;
