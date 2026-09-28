namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// The recorded <c>COUNTED_BALANCE</c> does not equal the computed closing cash balance of the
/// period — بخش ۳-ب (<c>docs/tankhah-خزanه-module.md</c> section 9): "countedBalance ثبت شده و =
/// مانده نقد پایان دوره". 409, a real data-conflict the caller must resolve (re-count the drawer
/// or investigate the discrepancy) before finalizing.
/// </summary>
public sealed class PettyCashSettlementCountedBalanceMismatchException : Exception
{
    public PettyCashSettlementCountedBalanceMismatchException(Guid fundId, decimal countedBalance, decimal closingCashBalance)
        : base($"Fund {fundId}: counted balance {countedBalance} does not match computed closing cash balance {closingCashBalance}.")
    {
        FundId = fundId;
        CountedBalance = countedBalance;
        ClosingCashBalance = closingCashBalance;
    }

    public Guid FundId { get; }

    public decimal CountedBalance { get; }

    public decimal ClosingCashBalance { get; }

    public string PublicDetail =>
        $"مبلغ شمارش‌شده ({CountedBalance}) با مانده نقد محاسبه‌شدهٔ پایان دوره ({ClosingCashBalance}) برابر نیست.";
}
