namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// SoD: the caller approving an انتقال وجه must not be its own creator — خزانه‌داری، بخش ۴-ج.
/// 409, conflict-of-interest, same posture as <c>PaymentRequestApproverConflictException</c>/
/// <c>PaymentRequestExecutorConflictException</c>.
/// </summary>
public sealed class TreasuryTransferApproverConflictException : Exception
{
    public TreasuryTransferApproverConflictException(Guid transferId)
        : base($"Caller cannot approve transfer {transferId} it created itself.")
    {
        TransferId = transferId;
    }

    public Guid TransferId { get; }

    public string PublicDetail => "ثبت‌کنندهٔ انتقال نمی‌تواند همان انتقال را تأیید کند.";
}
