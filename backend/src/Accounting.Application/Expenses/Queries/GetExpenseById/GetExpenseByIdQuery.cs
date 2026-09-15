using MediatR;

namespace Accounting.Application.Expenses.Queries.GetExpenseById;

/// <summary>
/// Returns a single <c>TB_EXPENCE</c> row projected to <see cref="ExpenseDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="ExpenseDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetExpenseByIdQuery(Guid Id) : IRequest<ExpenseDto?>;
