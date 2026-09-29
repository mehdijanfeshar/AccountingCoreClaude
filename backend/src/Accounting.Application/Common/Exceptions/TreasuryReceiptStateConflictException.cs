using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a دریافت وجه is not in a <c>STATE</c> an action (update/delete/register/cancel)
/// requires — خزانه‌داری، بخش ۴-ج. Same state-based-refusal shape as
/// <c>PaymentRequestStateConflictException</c>.
/// </summary>
public sealed class TreasuryReceiptStateConflictException : Exception
{
    public TreasuryReceiptStateConflictException(Guid receiptId, ReceiptState actualState, string requiredStateDescription)
        : base($"Receipt {receiptId} is in state {actualState}, but this action requires {requiredStateDescription}.")
    {
        ReceiptId = receiptId;
        ActualState = actualState;
    }

    public Guid ReceiptId { get; }

    public ReceiptState ActualState { get; }

    public string PublicDetail => $"دریافت وجه در وضعیت «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    internal static string StateLabel(ReceiptState state) => state switch
    {
        ReceiptState.Draft => "پیش‌نویس",
        ReceiptState.Registered => "ثبت‌شده",
        ReceiptState.Cancelled => "لغوشده",
        _ => "نامشخص",
    };
}
