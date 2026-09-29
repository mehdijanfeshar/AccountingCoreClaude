namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// The four rules that apply only at Submit (خزانه‌داری، بخش ۴-الف;
/// <c>docs/tankhah-khazaneh-module.md</c> §۱۰): the unit must have a <c>TB_TR_SETTING</c> row,
/// <c>DUE_DATE</c> must not already be in the past, an <c>INVOICE_REF</c> must be accompanied by
/// <c>INVOICE_APPROVED = true</c>, and (beneficiary national id, invoice ref) must not duplicate
/// another live request. Shared by <c>CreatePaymentRequestCommandHandler</c> (when
/// <c>submit = true</c>) and <c>SubmitPaymentRequestCommandHandler</c>, so the two paths cannot
/// drift apart — same shape as <c>IPettyCashSubmitRuleChecker</c>.
/// </summary>
public interface IPaymentRequestSubmitRuleChecker
{
    /// <summary>
    /// Throws the matching domain exception on the first rule that fails; does nothing when all
    /// four pass.
    /// </summary>
    /// <param name="paymentRequestId">Carried by the thrown exception only — for a not-yet-
    /// persisted request (Create+submit), this may be a <see cref="Guid.NewGuid"/> generated up
    /// front by the caller.</param>
    /// <param name="beneficiaryNationalId">May be null/empty — the duplicate check only applies
    /// when both this and <paramref name="invoiceRef"/> are non-empty.</param>
    /// <param name="invoiceRef">May be null/empty.</param>
    /// <param name="invoiceApproved">TB_TR_PAYMENT_REQUEST.INVOICE_APPROVED.</param>
    /// <param name="dueDate">TB_TR_PAYMENT_REQUEST.DUE_DATE (YYYYMMDD Jalali).</param>
    /// <param name="vahedCode">Caller's unit.</param>
    /// <param name="excludeId">Excluded from the duplicate check — the request's own id for an
    /// existing request being re-submitted, so it never matches itself; <see langword="null"/> for
    /// a brand-new request.</param>
    Task EnsureSubmittableAsync(
        Guid paymentRequestId,
        string? beneficiaryNationalId,
        string? invoiceRef,
        bool invoiceApproved,
        string dueDate,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);
}
