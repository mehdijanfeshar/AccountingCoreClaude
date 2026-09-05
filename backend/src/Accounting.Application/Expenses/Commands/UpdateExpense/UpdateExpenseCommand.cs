using MediatR;

namespace Accounting.Application.Expenses.Commands.UpdateExpense;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_EXPENCE</c> row (PUT semantics, not
/// PATCH) — mirrors <c>UpdateWorkShopCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// Field-by-field documentation is intentionally not repeated here — every field (including
/// <c>Id</c>, bound from the route and never the body) has the exact same column/type/length
/// meaning as the identically-named parameter on
/// <see cref="Accounting.Application.Expenses.Commands.CreateExpense.CreateExpenseCommand"/>; see
/// that command's XML doc for the misspelled-table-name/naming-normalisation note and the
/// FK/UNIQUE flags.
/// </summary>
public sealed record UpdateExpenseCommand(
    Guid Id,
    string ExpenseCode,
    string ExpenseName,
    string? Description,
    decimal? DefaultAmount,
    Guid? ExpenseGroupId,
    Guid? AccountCodeId,
    string? VahedCode) : IRequest;
