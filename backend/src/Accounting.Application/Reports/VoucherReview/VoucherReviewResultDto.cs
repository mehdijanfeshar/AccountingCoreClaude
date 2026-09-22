namespace Accounting.Application.Reports.VoucherReview;

/// <summary>
/// A page of مرور اسناد plus figures covering the <b>whole filtered set</b>.
///
/// <para>
/// The paging field names mirror <c>PagedResult&lt;T&gt;</c> exactly so the frontend's existing
/// paging reads it unchanged; it is a separate type only because of the three set-wide figures,
/// which a generic paged result has no place for.
/// </para>
///
/// <para>
/// ⚠️ Those figures are deliberately computed over every matching voucher, not over
/// <see cref="Items"/>. A total that only covered the current page would be arithmetic about an
/// arbitrary slice — a number that looks authoritative and means nothing.
/// </para>
/// </summary>
/// <param name="Items">The requested page, newest voucher first.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="TotalCount">Number of vouchers matching the filter.</param>
/// <param name="TotalDebtor">جمع کل بدهکار across every matching voucher.</param>
/// <param name="TotalCreditor">جمع کل بستانکار across every matching voucher.</param>
/// <param name="UnbalancedCount">
/// How many of those vouchers have بدهکار ≠ بستانکار. Counted server-side across the whole set
/// rather than derived per page, because "are any of my vouchers out of balance?" is a question
/// about the selection, not about whichever twenty rows happen to be on screen.
/// </param>
public sealed record VoucherReviewResultDto(
    IReadOnlyList<VoucherReviewRowDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal TotalDebtor,
    decimal TotalCreditor,
    int UnbalancedCount);
