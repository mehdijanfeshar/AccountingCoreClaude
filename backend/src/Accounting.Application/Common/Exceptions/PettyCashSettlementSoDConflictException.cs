namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Segregation of duties: the caller finalizing a settlement cannot be the <c>ADDUSERID</c>
/// (creator) of any صورت‌هزینه the settlement would cover — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). 409, same conflict-of-interest shape as
/// <c>PettyCashReplenishmentApproverConflictException</c>.
/// </summary>
public sealed class PettyCashSettlementSoDConflictException : Exception
{
    public PettyCashSettlementSoDConflictException(Guid fundId)
        : base($"Caller created at least one of the صورت‌هزینه rows the settlement of fund {fundId} would cover (segregation of duties).")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail =>
        "کاربر جاری نمی‌تواند تسویهٔ دوره‌ای را نهایی کند که خودش سازندهٔ یکی از صورت‌هزینه‌های منظورشده در آن است.";
}
