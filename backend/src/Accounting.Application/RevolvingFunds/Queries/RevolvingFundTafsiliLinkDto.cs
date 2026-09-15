namespace Accounting.Application.RevolvingFunds.Queries;

/// <summary>
/// Read-side projection of one active <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> row, nested inside its parent
/// <see cref="RevolvingFundDto"/> — this permanently embedded table has no read endpoint of its own,
/// mirroring the write side. Carries only the pair that identifies the assignment to a caller.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی.</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level the assignment sits at.</param>
public sealed record RevolvingFundTafsiliLinkDto(Guid TafsiliId, Guid LevelId);
