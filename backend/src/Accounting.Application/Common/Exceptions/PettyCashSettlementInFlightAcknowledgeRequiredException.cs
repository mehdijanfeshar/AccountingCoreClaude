namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// The fund has documents still "در جریان" (New/PendingReview/Returned — not yet decided) and the
/// caller did not set <c>acknowledgeInFlightTransfer: true</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). 400: this is a missing acknowledgement
/// flag on the request itself (fixable by resubmitting with the flag set), not a state conflict
/// the caller has no way out of.
/// </summary>
public sealed class PettyCashSettlementInFlightAcknowledgeRequiredException : Exception
{
    public PettyCashSettlementInFlightAcknowledgeRequiredException(Guid fundId, int inFlightCount, decimal inFlightAmount)
        : base($"Fund {fundId} has {inFlightCount} in-flight document(s) totalling {inFlightAmount}; acknowledgeInFlightTransfer must be true to finalize.")
    {
        FundId = fundId;
        InFlightCount = inFlightCount;
        InFlightAmount = inFlightAmount;
    }

    public Guid FundId { get; }

    public int InFlightCount { get; }

    public decimal InFlightAmount { get; }

    public string PublicDetail =>
        $"{InFlightCount} سند هنوز در جریان بررسی است (جمعاً {InFlightAmount}). برای ادامه، acknowledgeInFlightTransfer را true بفرستید.";
}
