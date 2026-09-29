using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary><c>GET api/treasury/payment-requests/{id}</c> — full detail + «گردش عملیات».</summary>
/// <param name="BeneficiaryTafsiliCode">Display — TB_TAFSILI.TAFSILI_CODE for <paramref name="BeneficiaryTafsiliId"/>, when set.</param>
/// <param name="BeneficiaryTafsiliName">Display — TB_TAFSILI.TAFSILI_NAME for <paramref name="BeneficiaryTafsiliId"/>, when set.</param>
/// <param name="CostCenterTafsilis">
/// اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — یک ردیف به‌ازای هر سطح تفصیلی مرکز هزینه، جایگزین ستون تک‌سطحی
/// حذف‌شدهٔ <c>CostCenterTafsiliId</c>.
/// </param>
public sealed record PaymentRequestDto(
    Guid Id,
    string Code,
    string BeneficiaryName,
    string? BeneficiaryNationalId,
    Guid? BeneficiaryTafsiliId,
    string? BeneficiaryTafsiliCode,
    string? BeneficiaryTafsiliName,
    TreasuryPaymentType PaymentType,
    string? InvoiceRef,
    bool InvoiceApproved,
    Guid ExpenseAccountId,
    IReadOnlyList<PaymentRequestCostCenterTafsiliDto> CostCenterTafsilis,
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
    /// <summary>بخش ۴-ب — سند «شناسایی بدهی» (شمارهٔ ۱)، در لحظهٔ تأیید نهایی صادر می‌شود.</summary>
    Guid? LiabilityVoucherId,
    string? LiabilityVoucherNumber,
    /// <summary>بخش ۴-ب — سند «پرداخت» (شمارهٔ ۲)، در لحظهٔ اجرا صادر می‌شود.</summary>
    Guid? PaymentVoucherId,
    string? PaymentVoucherNumber,
    string? BankReference,
    string? PaidDate,
    string? DestinationIban,
    string? ExecutedBy,
    DateTime? ExecutedDate,
    string? SuspendReason,
    IReadOnlyList<PaymentRequestEventDto> Events);
