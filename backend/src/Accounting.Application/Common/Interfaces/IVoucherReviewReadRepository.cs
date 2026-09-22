using Accounting.Application.Reports.VoucherReview;
using Accounting.Application.Reports.VoucherReview.GetVoucherReview;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read side of مرور اسناد.
///
/// <para>
/// ⚠️ Backed by TB_VOUCHERSHEAD/TB_VOUCHERSDETAIL, not by the view that exists for it — a
/// recorded exception to team working-rule #2, because <c>VW_VOUCHERREVIEW</c> double-counts
/// lines and can report a balanced voucher as unbalanced. The full argument, with the offending
/// SQL, is on <c>VoucherReviewReadRepository</c>.
/// </para>
/// </summary>
public interface IVoucherReviewReadRepository
{
    /// <summary>
    /// Returns one page of vouchers matching the query, newest first, together with the
    /// بدهکار/بستانکار totals of the entire filtered set — not of the returned page.
    /// </summary>
    Task<VoucherReviewResultDto> GetAsync(
        GetVoucherReviewQuery query,
        CancellationToken cancellationToken = default);
}
