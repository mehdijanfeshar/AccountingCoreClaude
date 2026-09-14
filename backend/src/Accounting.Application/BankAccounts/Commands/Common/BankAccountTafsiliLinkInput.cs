namespace Accounting.Application.BankAccounts.Commands.Common;

/// <summary>
/// One <c>TB_ACCOUNT_LINK_TAFSILI</c> row (a تفصیلی assignment on a bank account, at one
/// تفصیلی level of the account's معین), carried as part of its parent <c>TB_ACCOUNT</c> write —
/// never on its own. Shared by
/// <see cref="Accounting.Application.BankAccounts.Commands.CreateBankAccount.CreateBankAccountCommand"/>
/// and <see cref="Accounting.Application.BankAccounts.Commands.UpdateBankAccount.UpdateBankAccountCommand"/>,
/// hence the shared <c>Commands/Common</c> folder — modelled exactly on
/// <c>Vouchers.Commands.Common.VoucherDetailTafsiliLinkInput</c>.
///
/// <b>This is NOT independent CRUD for <c>TB_ACCOUNT_LINK_TAFSILI</c>.</b> The team rule
/// (2026-08-20, <c>docs/tamin-core-entity-reference.md</c> بخش ۵) that every
/// <c>*_LINK_TAFSIL*</c>/<c>*_LINK_LEVEL*</c> table stays permanently embedded is untouched:
/// this is a transport DTO nested inside the parent aggregate's command — no link repository,
/// no controller, no MediatR request of its own, exactly the shape
/// <c>NoIndependentLinkTableWritePathTests</c> locks in.
///
/// Deliberately carries no <c>Id</c>, no <c>AccountId</c> and no <c>VahedCode</c>: identity is
/// generated server-side, and the other two are derived by the handler from the parent bank
/// account being written in the same call — so a link can never disagree with its own parent
/// about which account or unit it belongs to. A caller identifies a link by its
/// <see cref="TafsiliId"/>/<see cref="LevelId"/> pair, which is what the update reconcile
/// matches on.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column — the assigned تفصیلی. Required (non-nullable column).</param>
/// <param name="LevelId">LEVEL_ID column — the تفصیلی level this assignment sits at. Required (non-nullable column).</param>
public sealed record BankAccountTafsiliLinkInput(
    Guid TafsiliId,
    Guid LevelId);
