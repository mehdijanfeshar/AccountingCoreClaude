namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// The settlement's current Draft period has no <c>COUNTED_BALANCE</c> recorded yet — the caller
/// must call <c>POST settlement/count</c> before <c>finalize</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). 409: the period exists but is not in the
/// state finalize requires, same shape as <c>PettyCashReplenishmentStateConflictException</c>.
/// </summary>
public sealed class PettyCashSettlementCountedBalanceRequiredException : Exception
{
    public PettyCashSettlementCountedBalanceRequiredException(Guid fundId, Guid periodId)
        : base($"Settlement period {periodId} of fund {fundId} has no COUNTED_BALANCE recorded yet.")
    {
        FundId = fundId;
        PeriodId = periodId;
    }

    public Guid FundId { get; }

    public Guid PeriodId { get; }

    public string PublicDetail => "پیش از نهایی‌سازی، ابتدا باید شمارش صندوق دوره ثبت شود.";
}
