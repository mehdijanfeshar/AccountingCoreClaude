namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// One <c>TB_PC_FUND_LINK_TAFSILI</c> row, carried as part of a full-replace
/// <c>UpsertPettyCashFundTafsilisCommand</c> — never on its own (same permanently-embedded shape
/// as <c>Accounting.Application.Expenses.Commands.Common.ExpenseTafsiliLinkInput</c>).
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column. Required.</param>
/// <param name="LevelId">LEVEL_ID column. Required.</param>
public sealed record PettyCashFundTafsiliLinkInput(Guid TafsiliId, Guid LevelId);
