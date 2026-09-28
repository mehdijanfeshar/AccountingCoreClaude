using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a ترمیم is not in the exact <c>STATE</c> an action (submit/approve/reject/
/// record-payment/delete) requires as its source state — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>). Same state-based-refusal shape as
/// <c>PettyCashReviewStateConflictException</c>.
/// </summary>
public sealed class PettyCashReplenishmentStateConflictException : Exception
{
    public PettyCashReplenishmentStateConflictException(
        Guid replenishmentId,
        PettyCashReplenishmentState actualState,
        PettyCashReplenishmentState requiredState)
        : base(
            $"Petty-cash replenishment {replenishmentId} is in state {actualState}, but this " +
            $"action requires it to be in state {requiredState}.")
    {
        ReplenishmentId = replenishmentId;
        ActualState = actualState;
        RequiredState = requiredState;
    }

    public Guid ReplenishmentId { get; }

    public PettyCashReplenishmentState ActualState { get; }

    public PettyCashReplenishmentState RequiredState { get; }

    public string PublicDetail => $"ترمیم در وضعیت «{StateLabel(ActualState)}» است و این اقدام روی آن ممکن نیست.";

    private static string StateLabel(PettyCashReplenishmentState state) => state switch
    {
        PettyCashReplenishmentState.Draft => "پیش‌نویس",
        PettyCashReplenishmentState.PendingFinanceManager => "در انتظار تأیید مدیر مالی",
        PettyCashReplenishmentState.PendingTreasurer => "در انتظار اقدام خزانه‌دار",
        PettyCashReplenishmentState.Paid => "پرداخت‌شده",
        PettyCashReplenishmentState.Rejected => "ردشده",
        _ => "نامشخص",
    };
}
