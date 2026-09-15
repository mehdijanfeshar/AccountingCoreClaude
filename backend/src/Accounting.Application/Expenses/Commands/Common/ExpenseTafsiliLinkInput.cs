namespace Accounting.Application.Expenses.Commands.Common;

/// <summary>
/// One <c>TB_EXPENCE_LINK_TAFSILI</c> row (a تفصیلی assignment on an expense, at one تفصیلی level
/// of the expense's معین), carried as part of its parent <c>TB_EXPENCE</c> write — never on its
/// own. Shared by
/// <see cref="Accounting.Application.Expenses.Commands.CreateExpense.CreateExpenseCommand"/> and
/// <see cref="Accounting.Application.Expenses.Commands.UpdateExpense.UpdateExpenseCommand"/>.
///
/// Same sanctioned embedded-table shape as
/// <c>BankAccounts.Commands.Common.BankAccountTafsiliLinkInput</c> — see that file for the full
/// rationale and the team rule (<c>docs/tamin-core-entity-reference.md</c> بخش ۵) it follows:
/// no link repository, no controller, no MediatR request of its own.
///
/// Carries no <c>Id</c>, no <c>ExpenseId</c> and no <c>VahedCode</c>: identity is server-generated
/// and the other two are derived by the handler from the parent expense written in the same call.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی. Required (non-nullable column).</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level this assignment sits at. Required (non-nullable column).</param>
public sealed record ExpenseTafsiliLinkInput(
    Guid TafsiliId,
    Guid LevelId);
