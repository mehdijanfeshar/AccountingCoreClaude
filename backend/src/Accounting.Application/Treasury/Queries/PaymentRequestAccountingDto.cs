using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// <c>GET api/treasury/payment-requests/{id}/accounting</c> response — خزانه‌داری، بخش ۴-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Read-only projection of the two automatic GL
/// vouchers (اگر صادر شده باشند) و کد Legacy <c>TB_PAYRECIVHEAD</c> (اگر پرداخت اجرا شده باشد).
/// </summary>
public sealed record PaymentRequestAccountingDto(
    PaymentRequestVoucherAccountingDto? LiabilityVoucher,
    PaymentRequestVoucherAccountingDto? PaymentVoucher,
    string? PayRecivCode);

/// <param name="TotalDebit">جمع بدهکار ردیف‌ها — سرور محاسبه می‌کند، همیشه با <see cref="TotalCredit"/> برابر است.</param>
public sealed record PaymentRequestVoucherAccountingDto(
    Guid Id,
    string? VoucherNumber,
    string? Date,
    DocLife? State,
    IReadOnlyList<PaymentRequestVoucherLineAccountingDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit);

/// <param name="TafsiliLabels">
/// هر تفصیلی به شکل <c>"{code} - {name}"</c> (همان قالب <c>VoucherDetailTafsiliLinkDto.Label</c>)،
/// با «، » به هم پیوسته؛ رشتهٔ خالی یعنی ردیف تفصیلی ندارد.
/// </param>
public sealed record PaymentRequestVoucherLineAccountingDto(
    string? AccountCode,
    string? AccountName,
    string TafsiliLabels,
    decimal Debit,
    decimal Credit);
