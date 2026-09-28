namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Closes the بخش ۳-ب TODO explicitly flagged on <c>DeletePettyCashRefundCommand</c>: an
/// استرداد whose own <c>REFUND_DATE</c> falls inside an already-
/// <see cref="Accounting.Domain.ValueObjects.PettyCashSettlementState.Final"/> period of its fund
/// may not be deleted — the period's voucher already reflects a cash movement that assumed this
/// row was still active. 409, same "finalized, no way back" shape as a voucher's own
/// <c>VoucherNotEditableException</c>.
/// </summary>
public sealed class PettyCashRefundLockedBySettledPeriodException : Exception
{
    public PettyCashRefundLockedBySettledPeriodException(Guid refundId)
        : base($"Refund {refundId} falls inside an already-finalized settlement period and cannot be deleted.")
    {
        RefundId = refundId;
    }

    public Guid RefundId { get; }

    public string PublicDetail => "این استرداد در دوره‌ای است که تسویهٔ آن نهایی شده؛ قابل حذف نیست.";
}
