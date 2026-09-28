namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.PettyCash.Commands.Common.PettyCashFinalApprovalService</c>
/// when the caller giving final approval is the very inspector who verified this document's
/// control (<c>TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID</c>) — a second segregation-of-duties rule for
/// تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸)، alongside the existing creator-vs-reviewer one
/// (<see cref="PettyCashSelfReviewConflictException"/>).
///
/// <b>409, not 403</b> — same reasoning as <see cref="PettyCashSelfReviewConflictException"/>: the
/// caller genuinely holds a final-approval-capable role, this specific document simply conflicts
/// with that role because they already verified it themselves.
/// </summary>
public sealed class PettyCashVerifierCannotApproveException : Exception
{
    public PettyCashVerifierCannotApproveException(Guid expenseDocId)
        : base(
            $"Caller verified expense document {expenseDocId} and cannot also give its final " +
            "approval (segregation of duties).")
    {
        ExpenseDocId = expenseDocId;
    }

    public Guid ExpenseDocId { get; }

    public string PublicDetail => "کسی که کنترل سند را تأیید کرده، نمی‌تواند تأییدکنندهٔ نهایی همان سند باشد.";
}
