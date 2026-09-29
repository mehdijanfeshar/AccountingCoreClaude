using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One row of <c>GET api/treasury/payment-requests</c>.</summary>
public sealed record PaymentRequestListItemDto(
    Guid Id,
    string Code,
    string BeneficiaryName,
    TreasuryPaymentType PaymentType,
    decimal NetPayableAmount,
    PaymentRequestState RequestState,
    string DueDate,
    DateTime? SubmittedDate,
    DateTime CreatedDate,
    string AddUserId);
