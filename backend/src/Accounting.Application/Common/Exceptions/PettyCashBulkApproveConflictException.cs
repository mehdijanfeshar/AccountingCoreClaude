namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>BulkApprovePettyCashExpenseDocsCommandHandler</c> when at least one requested
/// <c>id</c> fails its own single-document Approve check (not found, not an active reviewer,
/// self-review conflict, or wrong <c>DOC_STATE</c>). All-or-nothing: the handler validates every
/// id BEFORE calling <c>IUnitOfWork.SaveChangesAsync</c>, so throwing here — before that call —
/// guarantees no document in the batch is committed, per
/// <c>docs/tankhah-khazaneh-module.md</c>'s تصمیم‌های بخش ۲ ("all-or-nothing، هیچ سندی commit
/// نمی‌شود").
///
/// <b>409</b>, regardless of which underlying reason(s) caused each individual failure — the
/// per-id reason is preserved in <see cref="Failures"/> so <c>GlobalExceptionHandler</c> can echo
/// it back as a <c>failedIds</c> extension on the <c>ProblemDetails</c> body, but the overall
/// batch response is always a single Conflict, not a mix of statuses.
/// </summary>
public sealed class PettyCashBulkApproveConflictException : Exception
{
    public PettyCashBulkApproveConflictException(IReadOnlyDictionary<Guid, string> failures)
        : base($"Bulk approve failed for {failures.Count} document(s): {string.Join(", ", failures.Keys)}.")
    {
        Failures = failures;
    }

    /// <summary>Expense document id → machine-readable reason code (e.g. <c>"not-found"</c>,
    /// <c>"forbidden"</c>, <c>"self-review"</c>, <c>"invalid-state"</c>).</summary>
    public IReadOnlyDictionary<Guid, string> Failures { get; }

    public string PublicDetail => $"{Failures.Count} سند از تأیید گروهی رد شد و هیچ سندی تأیید نشد.";
}
