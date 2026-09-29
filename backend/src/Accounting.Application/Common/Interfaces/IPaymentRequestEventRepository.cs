using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_PAYMENT_REQUEST_EVENT"/> ("گردش عملیات") — insert-
/// only, same shape as <see cref="IPettyCashDocEventRepository"/>. Every write handler stages its
/// event alongside the request row it also touches, so both are persisted by the same
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call.
/// </summary>
public interface IPaymentRequestEventRepository
{
    Task AddAsync(TB_TR_PAYMENT_REQUEST_EVENT paymentRequestEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recent <see cref="Accounting.Domain.ValueObjects.PaymentRequestEventAction.Approve"/>
    /// event for <paramref name="paymentRequestId"/>, or <see langword="null"/> when the request has
    /// never been approved yet — used by the "یک کاربر دو مرحلهٔ متوالی را تأیید نکند" SoD check
    /// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
    /// </summary>
    Task<TB_TR_PAYMENT_REQUEST_EVENT?> GetLastApproveEventAsync(
        Guid paymentRequestId, CancellationToken cancellationToken = default);
}
