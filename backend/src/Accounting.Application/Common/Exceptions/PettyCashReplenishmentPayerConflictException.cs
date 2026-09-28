namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>RecordPettyCashReplenishmentPaymentCommandHandler</c> when the caller —
/// although a valid خزانه‌دار for the fund — is also the ترمیم's own approver
/// (<c>APPROVED_BY_USERID</c>). SoD: «فقط خزانه‌دار؛ ≠ تأییدکننده» (بخش ۳-الف،
/// <c>docs/tankhah-khazaneh-module.md</c>).
///
/// <b>409, not 403</b> — same conflict-of-interest shape as
/// <c>PettyCashReplenishmentApproverConflictException</c>.
/// </summary>
public sealed class PettyCashReplenishmentPayerConflictException : Exception
{
    public PettyCashReplenishmentPayerConflictException(Guid replenishmentId)
        : base($"Caller approved replenishment {replenishmentId} and cannot also record its payment (segregation of duties).")
    {
        ReplenishmentId = replenishmentId;
    }

    public Guid ReplenishmentId { get; }

    public string PublicDetail => "تأییدکنندهٔ ترمیم نمی‌تواند ثبت‌کنندهٔ پرداخت همان ترمیم باشد.";
}
