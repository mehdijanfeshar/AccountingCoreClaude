namespace Accounting.Application.Reports.AccountJournal;

/// <summary>
/// One posting line of دفتر روزنامه — a single ردیف سند in chronological order.
///
/// <para>
/// Unlike تراز آزمایشی and گزارش ماتریسی, nothing here is aggregated: the journal's whole purpose
/// is to show every movement in sequence, so each row is one line of one voucher.
/// </para>
/// </summary>
/// <param name="VoucherNumber">شماره سند.</param>
/// <param name="VoucherDate">تاریخ سند, Jalali <c>YYYYMMDD</c>.</param>
/// <param name="AccountCode">کد معین.</param>
/// <param name="AccountName">نام حساب معین.</param>
/// <param name="Description">شرح ردیف.</param>
/// <param name="DocLife">وضعیت سند as the raw ordinal (1..4); the UI maps it to a label.</param>
/// <param name="Debtor">بدهکار.</param>
/// <param name="Creditor">بستانکار.</param>
public sealed record AccountJournalRowDto(
    string VoucherNumber,
    string VoucherDate,
    string AccountCode,
    string AccountName,
    string Description,
    int? DocLife,
    decimal Debtor,
    decimal Creditor);
