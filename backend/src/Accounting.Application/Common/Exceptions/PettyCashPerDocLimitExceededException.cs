namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>SubmitPettyCashExpenseDocCommandHandler</c> when a document's total amount
/// exceeds its fund's configured <c>TB_PC_FUND_SETTING.PER_DOC_LIMIT</c>
/// (<c>docs/tankhah-khazaneh-module.md</c> §4: "مبلغ کل ≤ «سقف هر سند» تنخواه (اگر تعریف
/// شده)"). Only checked at Submit, and only when a per-document limit is actually configured —
/// a fund with no limit set has none to exceed. 400: the request itself (this specific amount,
/// on this specific fund, right now) is invalid, not a conflict with another resource.
/// </summary>
public sealed class PettyCashPerDocLimitExceededException : Exception
{
    public PettyCashPerDocLimitExceededException(Guid expenseDocId, decimal totalAmount, decimal perDocLimit)
        : base($"Petty-cash expense document {expenseDocId} total {totalAmount} exceeds the " +
               $"fund's per-document limit {perDocLimit}.")
    {
        ExpenseDocId = expenseDocId;
        TotalAmount = totalAmount;
        PerDocLimit = perDocLimit;
    }

    public Guid ExpenseDocId { get; }

    public decimal TotalAmount { get; }

    public decimal PerDocLimit { get; }

    public string PublicDetail =>
        $"مبلغ کل سند ({TotalAmount:N0}) از سقف مجاز هر سند این تنخواه ({PerDocLimit:N0}) بیشتر است.";
}
