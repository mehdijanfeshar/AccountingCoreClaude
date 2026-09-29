using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a درخواست پرداخت is not in a <c>REQUEST_STATE</c> an action (submit/approve/return/
/// reject/update/delete) requires — خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c>
/// §۱۰). Same state-based-refusal shape as <c>PettyCashReplenishmentStateConflictException</c>.
/// </summary>
public sealed class PaymentRequestStateConflictException : Exception
{
    public PaymentRequestStateConflictException(Guid paymentRequestId, PaymentRequestState actualState, string requiredStateDescription)
        : base($"Payment request {paymentRequestId} is in state {actualState}, but this action requires {requiredStateDescription}.")
    {
        PaymentRequestId = paymentRequestId;
        ActualState = actualState;
    }

    public Guid PaymentRequestId { get; }

    public PaymentRequestState ActualState { get; }

    public string PublicDetail => $"درخواست پرداخت در وضعیت «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    internal static string StateLabel(PaymentRequestState state) => state switch
    {
        PaymentRequestState.Draft => "پیش‌نویس",
        PaymentRequestState.PendingUnitManager => "در انتظار تأیید مدیر واحد",
        PaymentRequestState.PendingFinanceManager => "در انتظار تأیید مدیر مالی",
        PaymentRequestState.PendingCeo => "در انتظار تأیید مدیرعامل",
        PaymentRequestState.ReadyForExecution => "آمادهٔ اجرا",
        PaymentRequestState.Returned => "برگشتی",
        PaymentRequestState.Rejected => "ردشده",
        _ => "نامشخص",
    };
}
