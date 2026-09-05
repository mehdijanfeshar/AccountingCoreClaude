namespace Accounting.Application.Expenses.Queries;

/// <summary>
/// Read-side projection of <c>TB_EXPENCE</c>. Used by both <c>GetExpenses</c> (list) and
/// <c>GetExpenseById</c> — the Domain entity never crosses the Application boundary. Every field
/// mirrors the identically-named parameter documented on
/// <see cref="Accounting.Application.Expenses.Commands.CreateExpense.CreateExpenseCommand"/>;
/// full FK/UNIQUE flags are not repeated here.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="ExpenseCode">EXPENCECODE column — part of <c>UK_EXPENSE_CODE</c>.</param>
/// <param name="ExpenseName">EXPENCENAME column.</param>
/// <param name="Description">DESCRIPTION column.</param>
/// <param name="DefaultAmount">DEFAULTAMOUNT column.</param>
/// <param name="ExpenseGroupId">EXPENCEGROUP_ID column — optional link to <c>TB_EXPENCEGROUP</c>.</param>
/// <param name="AccountCodeId">ACCOUNTCODE_ID column — optional link to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="VahedCode">VAHEDCODE column — part of <c>UK_EXPENSE_CODE</c>.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">Logical delete flag, exposed as-is.</param>
public sealed record ExpenseDto(
    Guid Id,
    string ExpenseCode,
    string ExpenseName,
    string? Description,
    decimal? DefaultAmount,
    Guid? ExpenseGroupId,
    Guid? AccountCodeId,
    string? VahedCode,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
