namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// <c>GET api/treasury/transfers/{id}/accounting</c> response — same voucher shape as
/// <c>GET payment-requests/{id}/accounting</c> (owner instruction, بخش ۴-ج). انتقال وجه has no
/// Legacy <c>TB_PAYRECIVHEAD</c> counterpart (it is a bank-to-bank move, not a payment/receipt
/// against a ذی‌نفع/مشتری) — just the one automatic GL voucher.
/// </summary>
public sealed record TransferAccountingDto(PaymentRequestVoucherAccountingDto? Voucher);
