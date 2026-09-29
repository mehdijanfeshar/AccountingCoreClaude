using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Builds (and stages, never saves) the automatic «انتقال» GL voucher for یک انتقال وجه —
/// خزانه‌داری، بخش ۴-ج (owner decision ۲۰۲۶-۰۹-۲۹؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰):
/// بدهکار حساب بانک معین مقصد (+ تفصیلی مقصد) = <c>AMOUNT</c> · بستانکار حساب بانک معین مبدأ (+
/// تفصیلی مبدأ) = <c>AMOUNT</c>. هر دو تفصیلی مستقیماً از <c>TB_ACCOUNT_LINK_TAFSILI</c> کپی
/// می‌شوند (بدون منطق cardinality — همان الگوی خطوط بانکی سند «پرداخت» بخش ۴-ب). همیشه «موقت».
/// Only called by <c>approve</c>, AFTER both blocking checks (موجودی مبدأ، سقف روزانه) pass.
/// </summary>
public interface ITransferVoucherBuilder
{
    Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_TRANSFER transfer, string vahedCode, CancellationToken cancellationToken = default);
}
