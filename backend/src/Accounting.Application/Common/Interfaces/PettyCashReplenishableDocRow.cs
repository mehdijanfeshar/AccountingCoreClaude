namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// One صورت‌هزینه eligible to be linked into a new ترمیم — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>). See <see cref="IPettyCashExpenseDocRepository.GetApprovedUnlinkedByFundAsync"/>.
/// </summary>
/// <param name="ExpenseDocId">TB_PC_EXPENSE_DOC.ID.</param>
/// <param name="CostId">TB_CHARGEANDCOST_DETAIL.ID — becomes TB_CHARGE_LINK_COST.COST_ID.</param>
/// <param name="Amount">AMOUNT_BEFORE_TAX + VAT_AMOUNT — becomes TB_CHARGE_LINK_COST.AMOUNT.</param>
/// <param name="AccountCodeId">TB_EXPENCE.ACCOUNTCODE_ID (via the document's TB_CHARGEANDCOST_DETAIL.EXPENSE_ID) — nullable.</param>
public sealed record PettyCashReplenishableDocRow(
    Guid ExpenseDocId,
    Guid CostId,
    decimal Amount,
    Guid? AccountCodeId);
