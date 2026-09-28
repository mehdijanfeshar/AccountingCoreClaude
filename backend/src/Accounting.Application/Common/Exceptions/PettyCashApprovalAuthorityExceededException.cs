namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.PettyCash.Commands.Common.PettyCashFinalApprovalService</c>
/// when the caller's only qualifying role is
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/> and the document's
/// total amount exceeds <c>TB_PC_FUND.FINANCE_MANAGER_APPROVAL_LIMIT</c> — final approval above
/// that limit requires <see cref="Accounting.Domain.ValueObjects.PettyCashRole.ChiefExecutive"/>
/// (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸، صفحهٔ ۱۳ پاورپوینت).
///
/// <b>403, not 409</b> — distinct from <see cref="PettyCashReviewerAccessDeniedException"/>: the
/// caller genuinely holds a final-approval-capable role for this fund, just not one with enough
/// authority for this specific amount, so the public message names the real reason rather than a
/// generic "not a reviewer".
/// </summary>
public sealed class PettyCashApprovalAuthorityExceededException : Exception
{
    public PettyCashApprovalAuthorityExceededException(Guid expenseDocId, decimal amount, decimal limit)
        : base(
            $"Petty-cash expense document {expenseDocId} amount {amount} exceeds the caller's " +
            $"final-approval authority limit {limit}.")
    {
        ExpenseDocId = expenseDocId;
        Amount = amount;
        Limit = limit;
    }

    public Guid ExpenseDocId { get; }

    public decimal Amount { get; }

    public decimal Limit { get; }

    public string PublicDetail =>
        "مبلغ این سند بیش از سقف اختیار تأیید شماست؛ تأیید نهایی این سند فقط با مدیرعامل ممکن است.";
}
