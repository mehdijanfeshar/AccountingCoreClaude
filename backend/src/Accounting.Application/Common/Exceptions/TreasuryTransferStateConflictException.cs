using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when an انتقال وجه is not in a <c>STATE</c> an action (update/delete/submit/approve/
/// return/reject) requires — خزانه‌داری، بخش ۴-ج. Same state-based-refusal shape as
/// <c>PaymentRequestStateConflictException</c>.
/// </summary>
public sealed class TreasuryTransferStateConflictException : Exception
{
    public TreasuryTransferStateConflictException(Guid transferId, TransferState actualState, string requiredStateDescription)
        : base($"Transfer {transferId} is in state {actualState}, but this action requires {requiredStateDescription}.")
    {
        TransferId = transferId;
        ActualState = actualState;
    }

    public Guid TransferId { get; }

    public TransferState ActualState { get; }

    public string PublicDetail => $"انتقال وجه در وضعیت «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    internal static string StateLabel(TransferState state) => state switch
    {
        TransferState.Draft => "پیش‌نویس",
        TransferState.PendingTreasurer => "در انتظار اقدام خزانه‌دار",
        TransferState.Executed => "اجراشده",
        TransferState.Returned => "برگشتی",
        TransferState.Rejected => "ردشده",
        _ => "نامشخص",
    };
}
