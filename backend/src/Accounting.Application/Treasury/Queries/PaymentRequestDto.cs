using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary><c>GET api/treasury/payment-requests/{id}</c> — full detail + «گردش عملیات».</summary>
public sealed record PaymentRequestDto(
    Guid Id,
    string Code,
    string BeneficiaryName,
    string? BeneficiaryNationalId,
    Guid? BeneficiaryTafsiliId,
    TreasuryPaymentType PaymentType,
    string? InvoiceRef,
    bool InvoiceApproved,
    Guid ExpenseAccountId,
    Guid? CostCenterTafsiliId,
    decimal AmountBeforeTax,
    decimal? VatPercent,
    decimal VatAmount,
    decimal? InsuranceDeductionPercent,
    decimal InsuranceDeductionAmount,
    decimal NetPayableAmount,
    string DueDate,
    Guid PaymentAccountId,
    TreasuryPaymentMethod? PaymentMethod,
    string? Description,
    PaymentRequestState RequestState,
    DateTime? SubmittedDate,
    Guid? PayRecivHeadId,
    string AddUserId,
    DateTime CreatedDate,
    IReadOnlyList<PaymentRequestEventDto> Events);
