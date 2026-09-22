namespace Accounting.Application.Reports.VoucherReview;

/// <summary>
/// One سرسند in مرور اسناد, with both of its sides summed.
///
/// <para>
/// <b><see cref="Debtor"/> and <see cref="Creditor"/> are gross totals, not a net balance.</b> A
/// balanced voucher shows the same number twice; an unbalanced one shows two different numbers,
/// and the difference is the imbalance. Netting them — which is what the unusable
/// <c>VW_VOUCHERREVIEW</c> view does — would collapse every healthy voucher to a pair of zeros and
/// throw away the amounts the reader actually wants to see.
/// </para>
///
/// <para>
/// Nothing server-side rejects an unbalanced voucher (risk #3: the balance invariant was
/// deliberately dropped with the Rich model), so surfacing these two numbers is the only control
/// that exists.
/// </para>
/// </summary>
/// <param name="Id">شناسهٔ سرسند — lets the UI link to the existing voucher view page.</param>
/// <param name="VoucherNumber">شماره سند.</param>
/// <param name="SystemName">نام سیستم (نوع سند).</param>
/// <param name="AtfNo">شماره عطف.</param>
/// <param name="Description">شرح سند.</param>
/// <param name="DocLife">وضعیت سند as the raw ordinal (1..4); the UI maps it to a label.</param>
/// <param name="VoucherDate">تاریخ سند, Jalali <c>YYYYMMDD</c>.</param>
/// <param name="Year">سال مالی.</param>
/// <param name="Debtor">جمع بدهکار سند.</param>
/// <param name="Creditor">جمع بستانکار سند.</param>
public sealed record VoucherReviewRowDto(
    Guid Id,
    string VoucherNumber,
    string SystemName,
    string AtfNo,
    string Description,
    int? DocLife,
    string VoucherDate,
    string Year,
    decimal Debtor,
    decimal Creditor);
