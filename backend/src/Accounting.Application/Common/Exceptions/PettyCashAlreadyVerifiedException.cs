namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>VerifyPettyCashExpenseDocCommandHandler</c> when the document already carries a
/// <c>TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID</c> — verify may only happen once per بررسی cycle
/// (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸). A <c>Return</c> clears it, allowing a fresh <c>Verify</c> after
/// resubmission.
///
/// <b>409</b> — a state conflict, same shape as <see cref="PettyCashReviewStateConflictException"/>.
/// </summary>
public sealed class PettyCashAlreadyVerifiedException : Exception
{
    public PettyCashAlreadyVerifiedException(Guid expenseDocId)
        : base($"Petty-cash expense document {expenseDocId} has already been verified.")
    {
        ExpenseDocId = expenseDocId;
    }

    public Guid ExpenseDocId { get; }

    public string PublicDetail => "این سند قبلاً کنترل و تأیید شده است.";
}
