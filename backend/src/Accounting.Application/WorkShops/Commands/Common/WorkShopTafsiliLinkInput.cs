namespace Accounting.Application.WorkShops.Commands.Common;

/// <summary>
/// One <c>TB_WORKSHOP_LINK_TAFSILI</c> row (a تفصیلی assignment at one تفصیلی level of the
/// WorkShop's معین), carried as part of its parent <c>TB_WORKSHOP</c> write — never on its own.
///
/// Same sanctioned embedded-table shape as
/// <c>BankAccounts.Commands.Common.BankAccountTafsiliLinkInput</c> — see that file for the full
/// rationale and the team rule (<c>docs/tamin-core-entity-reference.md</c> بخش ۵) it follows:
/// no link repository, no controller, no MediatR request of its own.
///
/// Carries no <c>Id</c>, no <c>WorkShopId</c> and no <c>VahedCode</c>: identity is server-generated
/// and the other two are derived by the handler from the parent written in the same call.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی. Required (non-nullable column).</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level this assignment sits at. Required (non-nullable column).</param>
public sealed record WorkShopTafsiliLinkInput(
    Guid TafsiliId,
    Guid LevelId);
