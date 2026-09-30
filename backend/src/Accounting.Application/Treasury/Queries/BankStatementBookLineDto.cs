namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// One candidate/leftover دفتری (book) voucher-detail line for مغایرت‌گیری بانکی — خزانه‌داری،
/// بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Shared shape for
/// <c>GET statements/{id}/book-candidates</c>, the auto-match algorithm's candidate search, and
/// the statement-detail «book-only» (outstanding) list — all three are variations of "unmatched
/// voucher-detail lines of the bank's معین in a date range", never copied three times.
/// </summary>
/// <param name="SourceBankReference">
/// The BANK_REFERENCE recorded on the خزانه‌داری document (درخواست پرداخت/دریافت/انتقال) that
/// produced this voucher line, if resolvable — auto-match priority (۱). <see langword="null"/>
/// when the voucher was not produced by one of those three خزانه‌داری flows (e.g. a manual
/// voucher) or that document has no bank reference yet.
/// </param>
public sealed record BankStatementBookLineDto(
    Guid VoucherDetailId,
    Guid VoucherHeadId,
    string? VoucherNumber,
    string VoucherDate,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? SourceBankReference);
