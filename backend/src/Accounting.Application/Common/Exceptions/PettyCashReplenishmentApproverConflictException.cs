namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>ApprovePettyCashReplenishmentCommandHandler</c> when the caller — although a
/// valid مدیر مالی for the fund — is also the ترمیم's own creator (<c>ADDUSERID</c>). SoD: «فقط
/// نقش FinanceManager همان تنخواه؛ ≠ ایجادکننده» (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>).
///
/// <b>409, not 403</b> — same conflict-of-interest shape as <c>PettyCashSelfReviewConflictException</c>.
/// </summary>
public sealed class PettyCashReplenishmentApproverConflictException : Exception
{
    public PettyCashReplenishmentApproverConflictException(Guid replenishmentId)
        : base($"Caller created replenishment {replenishmentId} and cannot also approve it (segregation of duties).")
    {
        ReplenishmentId = replenishmentId;
    }

    public Guid ReplenishmentId { get; }

    public string PublicDetail => "ایجادکنندهٔ ترمیم نمی‌تواند تأییدکنندهٔ همان ترمیم باشد.";
}
