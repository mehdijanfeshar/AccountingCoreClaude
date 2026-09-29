namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Defensive guard for both بخش ۴-ب automatic vouchers («شناسایی بدهی» و «پرداخت») — closes open
/// risk #3 for this write path. By construction each voucher's lines are built so debit always
/// equals credit (voucher 1: <c>AMOUNT_BEFORE_TAX + VAT_AMOUNT == NET_PAYABLE_AMOUNT +
/// INSURANCE_DEDUCTION_AMOUNT</c>; voucher 2: both lines carry <c>NET_PAYABLE_AMOUNT</c>), so this
/// can never actually fire; it exists so a future change to that construction cannot silently post
/// an unbalanced GL voucher — same role as <c>PettyCashSettlementUnbalancedException</c>.
/// </summary>
public sealed class PaymentRequestVoucherUnbalancedException : Exception
{
    public PaymentRequestVoucherUnbalancedException(string voucherLabel, decimal totalDebtor, decimal totalCredit)
        : base($"{voucherLabel} voucher is unbalanced: debtor {totalDebtor} != credit {totalCredit}.")
    {
        VoucherLabel = voucherLabel;
        TotalDebtor = totalDebtor;
        TotalCredit = totalCredit;
    }

    public string VoucherLabel { get; }

    public decimal TotalDebtor { get; }

    public decimal TotalCredit { get; }

    public string PublicDetail => $"سند «{VoucherLabel}» تراز نیست؛ عملیات متوقف شد.";
}
