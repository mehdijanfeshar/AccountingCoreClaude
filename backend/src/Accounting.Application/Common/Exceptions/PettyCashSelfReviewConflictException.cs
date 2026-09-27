namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.PettyCash.Commands.Common.PettyCashReviewAuthorizer</c>
/// when the current caller — although a valid active reviewer for the fund — is also the very
/// user who created (<c>ADDUSERID</c>) the صورت‌هزینه they are trying to StartReview/Approve/
/// Return/Reject. Segregation of duties: «ایجادکننده ≠ بررسی‌کننده»
/// (<c>docs/tankhah-khazaneh-module.md</c>, تصمیم‌های بخش ۲، قاعدهٔ SoD جزء (ب)).
///
/// <b>409, not 403.</b> The caller genuinely holds reviewer access to this fund in general
/// (unlike <see cref="PettyCashReviewerAccessDeniedException"/>) — this specific document simply
/// conflicts with that role because they authored it themselves, a conflict-of-interest state
/// rather than an outright lack of permission.
/// </summary>
public sealed class PettyCashSelfReviewConflictException : Exception
{
    public PettyCashSelfReviewConflictException(Guid expenseDocId)
        : base($"Caller created expense document {expenseDocId} and cannot also review it (segregation of duties).")
    {
        ExpenseDocId = expenseDocId;
    }

    public Guid ExpenseDocId { get; }

    public string PublicDetail => "ایجادکنندهٔ سند نمی‌تواند بررسی‌کنندهٔ همان سند باشد.";
}
