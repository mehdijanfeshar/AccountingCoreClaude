namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.PettyCash.Commands.Common.PettyCashFinalApprovalService</c>
/// when final approval is attempted on a صورت‌هزینه whose control has not yet been verified by an
/// inspector (<c>TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID</c> is still <see langword="null"/>) —
/// تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، تأیید دومرحله‌ای، صفحهٔ ۱۲ پاورپوینت).
///
/// <b>409, not 400</b> — same state-based-refusal shape as
/// <see cref="PettyCashReviewStateConflictException"/>: the document IS in <c>PendingReview</c>
/// (otherwise that exception would already have fired), it simply has not completed the first of
/// the two required approval steps yet.
/// </summary>
public sealed class PettyCashNotVerifiedException : Exception
{
    public PettyCashNotVerifiedException(Guid expenseDocId)
        : base($"Petty-cash expense document {expenseDocId} has not been verified by an inspector yet.")
    {
        ExpenseDocId = expenseDocId;
    }

    public Guid ExpenseDocId { get; }

    public string PublicDetail => "ابتدا بازرس باید کنترل این سند را تأیید کند.";
}
