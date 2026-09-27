using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// An attempt to update or delete a <c>TB_PC_EXPENSE_DOC</c> row whose <c>DOC_STATE</c> does not
/// allow that action — thrown by
/// <see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>, the single place
/// this rule lives (same "one rule, one home" shape as <c>VoucherEditability</c>).
///
/// <b>The rule</b> (<c>docs/tankhah-khazaneh-module.md</c> §4): only
/// <see cref="PettyCashDocState.Draft"/> and <see cref="PettyCashDocState.Returned"/> may be
/// updated; only <see cref="PettyCashDocState.Draft"/> may be deleted.
///
/// <b>409, not 400 or 403</b> — same reasoning as <c>VoucherNotEditableException</c>: the caller
/// is entitled to edit/delete this document, just not while it is in this state, so the request
/// conflicts with the resource's current state rather than being malformed or forbidden outright.
/// </summary>
public sealed class PettyCashDocNotEditableException : Exception
{
    public PettyCashDocNotEditableException(Guid expenseDocId, PettyCashDocState state, string action)
        : base($"Petty-cash expense document {expenseDocId} is in state {state} and cannot be {action}.")
    {
        ExpenseDocId = expenseDocId;
        State = state;
        Action = action;
    }

    public Guid ExpenseDocId { get; }

    public PettyCashDocState State { get; }

    /// <summary>Either <c>"updated"</c> or <c>"deleted"</c> — drives <see cref="PublicDetail"/>.</summary>
    public string Action { get; }

    public string PublicDetail =>
        $"صورت‌هزینه در وضعیت «{StateLabel(State)}» است و قابل {ActionLabel(Action)} نیست.";

    private static string ActionLabel(string action) => action switch
    {
        "updated" => "ویرایش",
        "deleted" => "حذف",
        _ => action,
    };

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
