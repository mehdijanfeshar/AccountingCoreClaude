namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by انتقال وجه <c>approve</c> when the source bank account's current GL balance (owner
/// decision ۲۰۲۶-۰۹-۲۹: مانده حساب معین بانک مبدأ روی تمام اسناد غیرحذف‌شدهٔ سال، شامل موقت) is
/// less than the transfer amount — blocking control (a), خزانه‌داری، بخش ۴-ج.
/// </summary>
public sealed class TreasuryTransferInsufficientBalanceException : Exception
{
    public TreasuryTransferInsufficientBalanceException(Guid transferId, decimal currentBalance, decimal amount)
        : base($"Transfer {transferId} needs {amount} but source bank account balance is only {currentBalance}.")
    {
        TransferId = transferId;
        CurrentBalance = currentBalance;
        Amount = amount;
    }

    public Guid TransferId { get; }

    public decimal CurrentBalance { get; }

    public decimal Amount { get; }

    public string PublicDetail =>
        $"موجودی حساب بانکی مبدأ ({CurrentBalance:N0}) برای مبلغ انتقال ({Amount:N0}) کافی نیست.";
}
