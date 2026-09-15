namespace Accounting.Application.Expenses.Queries;

/// <summary>
/// Read-side projection of one active <c>TB_EXPENCE_LINK_TAFSILI</c> row, nested inside its parent
/// <see cref="ExpenseDto"/> — this permanently embedded table has no read endpoint of its own,
/// mirroring the write side. Carries only the pair that identifies the assignment to a caller.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی.</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level the assignment sits at.</param>
public sealed record ExpenseTafsiliLinkDto(Guid TafsiliId, Guid LevelId);
