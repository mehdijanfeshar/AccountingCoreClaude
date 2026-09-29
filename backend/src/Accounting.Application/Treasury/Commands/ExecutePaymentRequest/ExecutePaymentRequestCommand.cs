using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ExecutePaymentRequest;

/// <summary>
/// <c>POST api/treasury/payment-requests/{id}/execute</c> — خزانه‌داری، بخش ۴-ب (owner decision
/// ۲۰۲۶-۰۹-۲۹، <c>docs/tankhah-khazaneh-module.md</c> §۱۰). Records the manual bank payment the
/// treasurer already made (no bank integration/OTP), issues سند «پرداخت» (voucher 2) and writes
/// Legacy <c>TB_PAYRECIVHEAD/DETAIL</c>, then moves
/// <see cref="PaymentRequestState.ReadyForExecution"/> → <see cref="PaymentRequestState.Executed"/>
/// (terminal — no return/reject from here, سند شناسایی بدهی از قبل صادر شده).
/// Only a caller holding <see cref="TreasuryRole.Treasurer"/> in the unit, SoD (executor ≠
/// creator).
/// </summary>
/// <param name="Id">The درخواست پرداخت id (route parameter).</param>
/// <param name="BankReference">شمارهٔ پیگیری/مرجع بانکی — الزامی.</param>
/// <param name="PaidDate">تاریخ واقعی پرداخت، شمسی <c>YYYYMMDD</c> — الزامی؛ تاریخ سند «پرداخت» هم همین است.</param>
/// <param name="DestinationIban">شمارهٔ شبای مقصد — اختیاری (فرمت <c>IR</c> + ۲۴ رقم).</param>
/// <param name="PaymentMethod">اگر مقدار دارد، <c>PAYMENT_METHOD</c> درخواست را بازنویسی می‌کند؛ اگر خالی باشد مقدار درخواست دست‌نخورده می‌ماند.</param>
public sealed record ExecutePaymentRequestCommand(
    Guid Id,
    string BankReference,
    string PaidDate,
    string? DestinationIban,
    TreasuryPaymentMethod? PaymentMethod) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
