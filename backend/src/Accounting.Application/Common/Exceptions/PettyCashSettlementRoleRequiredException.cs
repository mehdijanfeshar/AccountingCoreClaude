namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller finalizing a settlement is not an active
/// <see cref="Accounting.Domain.ValueObjects.PettyCashRole.SeniorAccountant"/> reviewer of the
/// fund — بخش ۳-ب (<c>docs/tankhah-khazaneh-module.md</c> section 9): "فقط SeniorAccountant
/// (PettyCashRole=4) همان تنخواه". 403, a straight permissions gap — same shape as
/// <c>PettyCashReplenishmentRoleRequiredException</c>.
/// </summary>
public sealed class PettyCashSettlementRoleRequiredException : Exception
{
    public PettyCashSettlementRoleRequiredException(Guid fundId)
        : base($"Caller is not an active SeniorAccountant reviewer of petty-cash fund {fundId}.")
    {
        FundId = fundId;
    }

    public Guid FundId { get; }

    public string PublicDetail => "فقط حسابدار ارشد همین تنخواه مجاز به نهایی‌سازی تسویهٔ دوره است.";
}
