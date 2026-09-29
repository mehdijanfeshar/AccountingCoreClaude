using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Builds (and stages, never saves) the automatic «دریافت» GL voucher for یک دریافت وجه —
/// خزانه‌داری، بخش ۴-ج (owner decision ۲۰۲۶-۰۹-۲۹؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰):
/// بدهکار حساب بانک معین (+ تفصیلی بانک) = <c>AMOUNT</c> · بستانکار حساب «حساب‌های دریافتنی»
/// (تفصیلی پرداخت‌کننده در سطح الزامی — «همان قاعدهٔ cardinality resolver بستانکاران بخش ۴-ب») =
/// <c>AMOUNT</c>. همیشه «موقت» (<c>DOCLIFE=Temporary</c>, <c>ISAUTOMATIC=true</c>) — همان الگوی
/// هر سند خودکار این پروژه. Reuses <see cref="PaymentRequestVoucherBuildResult"/> (بخش ۴-ب) rather
/// than a module-specific clone — the result shape has nothing payment-request-specific in it.
/// </summary>
public interface IReceiptVoucherBuilder
{
    Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_RECEIPT receipt, string vahedCode, CancellationToken cancellationToken = default);
}
