namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by انتقال وجه <c>approve</c> when today's already-<see cref="Accounting.Domain.ValueObjects.TransferState.Executed"/>
/// transfers from the same source bank account, on the same <c>TRANSFER_DATE</c>, plus this
/// transfer's amount, would exceed <c>TB_TR_SETTING.DAILY_TRANSFER_LIMIT</c> — blocking control
/// (b), خزانه‌داری، بخش ۴-ج.
/// </summary>
public sealed class TreasuryTransferDailyLimitExceededException : Exception
{
    public TreasuryTransferDailyLimitExceededException(Guid transferId, decimal alreadyTransferredToday, decimal amount, decimal dailyLimit)
        : base($"Transfer {transferId}: {alreadyTransferredToday} + {amount} would exceed daily limit {dailyLimit}.")
    {
        TransferId = transferId;
        AlreadyTransferredToday = alreadyTransferredToday;
        Amount = amount;
        DailyLimit = dailyLimit;
    }

    public Guid TransferId { get; }

    public decimal AlreadyTransferredToday { get; }

    public decimal Amount { get; }

    public decimal DailyLimit { get; }

    public string PublicDetail =>
        $"مجموع انتقال‌های امروز از این حساب ({AlreadyTransferredToday:N0}) به‌علاوهٔ این انتقال ({Amount:N0}) از سقف روزانه ({DailyLimit:N0}) بیشتر می‌شود.";
}
