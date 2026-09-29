namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// <c>GET api/treasury/receipts/{id}/accounting</c> response — same voucher shape as
/// <c>GET payment-requests/{id}/accounting</c> (owner instruction, بخش ۴-ج), reusing
/// <see cref="PaymentRequestVoucherAccountingDto"/> rather than a near-identical clone (see
/// <c>IVoucherAccountingReader</c> XML doc).
/// </summary>
public sealed record ReceiptAccountingDto(
    PaymentRequestVoucherAccountingDto? Voucher,
    string? PayRecivCode);
