namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a caller who is not the owning صورت‌هزینه's <c>ADDUSERID</c> attempts to add or
/// remove a <c>TB_PC_ATTACHMENT</c> (<c>docs/tankhah-khazaneh-module.md</c>, تصمیم‌های بخش ۲،
/// پیوست: "افزودن/حذف محدود به مالک سند"). Deliberately no <c>IPettyCashReviewAuthorizer</c>
/// involvement here — unlike StartReview/Approve/Return/Reject, attachment add/delete is not a
/// بررسی‌کننده action at all, it is plain ownership.
///
/// <b>403, not 409</b> — same shape as <see cref="PettyCashReviewerAccessDeniedException"/>: this
/// is a straight permissions gap (the caller is simply not the document's owner), not a state
/// conflict the caller could resolve by acting differently.
/// </summary>
public sealed class PettyCashAttachmentOwnerOnlyException : Exception
{
    public PettyCashAttachmentOwnerOnlyException(Guid expenseDocId)
        : base($"Caller is not the owner of petty-cash expense document {expenseDocId}.")
    {
        ExpenseDocId = expenseDocId;
    }

    public Guid ExpenseDocId { get; }

    public string PublicDetail => "فقط مالک سند می‌تواند پیوست افزوده یا حذف کند.";
}
