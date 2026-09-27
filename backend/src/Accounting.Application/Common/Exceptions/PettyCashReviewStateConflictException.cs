using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.PettyCash.Commands.Common.PettyCashReviewTransitionService</c>
/// when a صورت‌هزینه is not in the exact <c>DOC_STATE</c> a review action (StartReview/Approve/
/// Return/Reject) requires as its source state — e.g. Approve requires
/// <see cref="PettyCashDocState.PendingReview"/>, and finds the document already
/// <see cref="PettyCashDocState.Approved"/> or still <see cref="PettyCashDocState.New"/>.
///
/// <b>409, not 400</b> — same reasoning as <see cref="PettyCashDocNotEditableException"/>: the
/// action is one the caller may legitimately perform on this kind of document, just not while it
/// is in this particular state, so it is a conflict with current resource state.
/// </summary>
public sealed class PettyCashReviewStateConflictException : Exception
{
    public PettyCashReviewStateConflictException(
        Guid expenseDocId,
        PettyCashDocState actualState,
        PettyCashDocState requiredState,
        PettyCashDocAction action)
        : base(
            $"Petty-cash expense document {expenseDocId} is in state {actualState}, but action " +
            $"{action} requires it to be in state {requiredState}.")
    {
        ExpenseDocId = expenseDocId;
        ActualState = actualState;
        RequiredState = requiredState;
        Action = action;
    }

    public Guid ExpenseDocId { get; }

    public PettyCashDocState ActualState { get; }

    public PettyCashDocState RequiredState { get; }

    public PettyCashDocAction Action { get; }

    public string PublicDetail =>
        $"صورت‌هزینه در وضعیت «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    private static string StateLabel(PettyCashDocState state) => state switch
    {
        PettyCashDocState.Draft => "پیش‌نویس",
        PettyCashDocState.New => "جدید",
        PettyCashDocState.PendingReview => "در انتظار بررسی",
        PettyCashDocState.Returned => "برگشتی",
        PettyCashDocState.Approved => "تأییدشده",
        PettyCashDocState.Rejected => "ردشده",
        PettyCashDocState.Settled => "تسویه‌شده",
        _ => "نامشخص",
    };
}
