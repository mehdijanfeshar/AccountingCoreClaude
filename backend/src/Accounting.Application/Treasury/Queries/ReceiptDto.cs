using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary><c>GET api/treasury/receipts/{id}</c> — full detail. No «گردش عملیات» (دریافت وجه has
/// no approval chain, unlike درخواست پرداخت/انتقال وجه) — <see cref="State"/> plus
/// <see cref="RegisteredBy"/>/<see cref="RegisteredDate"/> already capture its one meaningful
/// transition.</summary>
/// <param name="PayerTafsiliCode">Display — TB_TAFSILI.TAFSILI_CODE for <see cref="PayerTafsiliId"/>.</param>
/// <param name="PayerTafsiliName">Display — TB_TAFSILI.TAFSILI_NAME for <see cref="PayerTafsiliId"/>.</param>
public sealed record ReceiptDto(
    Guid Id,
    string Code,
    Guid PayerTafsiliId,
    string? PayerTafsiliCode,
    string? PayerTafsiliName,
    string PayerName,
    decimal Amount,
    Guid BankAccountId,
    TreasuryPaymentMethod ReceiptMethod,
    string ReceiptDate,
    string BankReference,
    string? InvoiceRef,
    string? Description,
    ReceiptState State,
    Guid? VoucherId,
    string? VoucherNumber,
    Guid? PayRecivHeadId,
    string? PayRecivCode,
    string? RegisteredBy,
    DateTime? RegisteredDate,
    string AddUserId,
    DateTime CreatedDate);
