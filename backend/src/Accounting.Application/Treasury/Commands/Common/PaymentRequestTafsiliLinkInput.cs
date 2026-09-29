namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// One <c>TB_TR_PAYMENT_REQUEST_LINK_TAFSILI</c> row, carried as part of a full-replace
/// <c>costCenterTafsilis</c> list on <c>CreatePaymentRequestCommand</c>/<c>UpdatePaymentRequestCommand</c>
/// — never on its own (same permanently-embedded shape as
/// <c>Accounting.Application.PettyCash.Commands.Common.PettyCashFundTafsiliLinkInput</c>).
/// Replaces the single <c>CostCenterTafsiliId</c> field removed by the 2026-09-29 owner decision
/// (اصلاح ۴-الف؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰) — the request now carries تفصیلی for
/// every level the request's <c>ExpenseAccountId</c> requires, not just the first.
/// </summary>
/// <param name="TafsiliId">TAFSILI_ID column. Required.</param>
/// <param name="LevelId">LEVEL_ID column. Required.</param>
public sealed record PaymentRequestTafsiliLinkInput(Guid TafsiliId, Guid LevelId);
