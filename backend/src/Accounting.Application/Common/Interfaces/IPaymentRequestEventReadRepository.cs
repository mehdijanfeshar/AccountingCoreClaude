using Accounting.Application.Treasury.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_PAYMENT_REQUEST_EVENT"/> —
/// backs the <c>events</c> slice of <c>GET api/treasury/payment-requests/{id}</c>.
/// </summary>
public interface IPaymentRequestEventReadRepository
{
    /// <summary>Every event for <paramref name="paymentRequestId"/>, oldest first.</summary>
    Task<IReadOnlyList<PaymentRequestEventDto>> GetByPaymentRequestIdAsync(
        Guid paymentRequestId, CancellationToken cancellationToken = default);
}
