namespace Accounting.Application.RevolvingFunds.Commands.Common;

/// <summary>
/// One <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> row (a تفصیلی assignment at one تفصیلی level of the
/// RevolvingFund's معین), carried as part of its parent <c>TB_REVOLVING_FUND</c> write — never on its own.
///
/// Same sanctioned embedded-table shape as
/// <c>BankAccounts.Commands.Common.BankAccountTafsiliLinkInput</c> — see that file for the full
/// rationale and the team rule (<c>docs/tamin-core-entity-reference.md</c> بخش ۵) it follows:
/// no link repository, no controller, no MediatR request of its own.
///
/// Carries no <c>Id</c>, no <c>RevolvingFundId</c> and no <c>VahedCode</c>: identity is server-generated
/// and the other two are derived by the handler from the parent written in the same call.
/// The link table also carries <c>YEAR</c>; the handler copies it from the parent revolving fund
/// rather than accepting it here, for the same reason the unit code is not accepted: a link must
/// never be able to disagree with its own parent.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی. Required (non-nullable column).</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level this assignment sits at. Required (non-nullable column).</param>
public sealed record RevolvingFundTafsiliLinkInput(
    Guid TafsiliId,
    Guid LevelId);
