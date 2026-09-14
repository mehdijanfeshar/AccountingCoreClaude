namespace Accounting.Application.BankAccounts.Queries;

/// <summary>
/// Read-side projection of one active <c>TB_ACCOUNT_LINK_TAFSILI</c> row, nested inside its
/// parent <see cref="BankAccountDto"/> — this permanently embedded table has no read endpoint of
/// its own, mirroring the write side.
///
/// Carries only the pair that identifies the assignment to a caller; <c>ID</c>, <c>ACCOUNT_ID</c>
/// and <c>VAHEDCODE</c> are deliberately omitted (server-owned, and redundant with the parent).
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی.</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level the assignment sits at.</param>
public sealed record BankAccountTafsiliLinkDto(Guid TafsiliId, Guid LevelId);
