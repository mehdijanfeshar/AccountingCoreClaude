namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>SubmitPettyCashExpenseDocCommandHandler</c> when a document's total amount
/// exceeds its fund's current cash balance (<c>docs/tankhah-khazaneh-module.md</c> §4: "مبلغ کل
/// ≤ موجودی نقد تنخواه"), computed by the §2 balance equation:
/// <c>موجودی نقد = DEFAULTAMOUNT − Σ(مبلغ کل اسناد در وضعیت New, PendingReview, Returned,
/// Approved)</c>, excluding the document being submitted itself. 400 for the same reason as
/// <see cref="PettyCashPerDocLimitExceededException"/>: this specific submission, right now, does
/// not fit — it is not a conflict with another resource.
/// </summary>
public sealed class PettyCashInsufficientCashBalanceException : Exception
{
    public PettyCashInsufficientCashBalanceException(Guid expenseDocId, decimal totalAmount, decimal cashBalance)
        : base($"Petty-cash expense document {expenseDocId} total {totalAmount} exceeds the " +
               $"fund's current cash balance {cashBalance}.")
    {
        ExpenseDocId = expenseDocId;
        TotalAmount = totalAmount;
        CashBalance = cashBalance;
    }

    public Guid ExpenseDocId { get; }

    public decimal TotalAmount { get; }

    public decimal CashBalance { get; }

    public string PublicDetail =>
        $"مبلغ کل سند ({TotalAmount:N0}) از موجودی نقد فعلی تنخواه ({CashBalance:N0}) بیشتر است.";
}
