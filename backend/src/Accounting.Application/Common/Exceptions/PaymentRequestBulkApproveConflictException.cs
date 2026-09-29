namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>BulkApprovePaymentRequestsCommandHandler</c> when at least one requested
/// <c>id</c> fails its own single-request Approve check (not found, wrong state, no role,
/// creator/consecutive-approver conflict, or over the unit's <c>BULK_APPROVE_LIMIT</c>). All-or-
/// nothing: the handler validates every id BEFORE calling
/// <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/>, so nothing
/// in the batch is committed when any one fails — same shape as
/// <c>PettyCashBulkApproveConflictException</c>.
/// </summary>
public sealed class PaymentRequestBulkApproveConflictException : Exception
{
    public PaymentRequestBulkApproveConflictException(IReadOnlyDictionary<Guid, string> failures)
        : base($"Bulk approve failed for {failures.Count} payment request(s): {string.Join(", ", failures.Keys)}.")
    {
        Failures = failures;
    }

    /// <summary>Payment request id → machine-readable reason code (<c>"not-found"</c>,
    /// <c>"forbidden"</c>, <c>"self-approve"</c>, <c>"consecutive-approver"</c>,
    /// <c>"invalid-state"</c>, <c>"over-bulk-limit"</c>).</summary>
    public IReadOnlyDictionary<Guid, string> Failures { get; }

    public string PublicDetail => $"{Failures.Count} درخواست از تأیید گروهی رد شد و هیچ‌کدام تأیید نشد.";
}
