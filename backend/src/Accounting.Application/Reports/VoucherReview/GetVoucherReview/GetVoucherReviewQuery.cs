using Accounting.Application.Common.Security;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Reports.VoucherReview.GetVoucherReview;

/// <summary>
/// مرور اسناد — a filtered, paged list of vouchers with each voucher's بدهکار/بستانکار totals.
/// READ-ONLY.
///
/// <para>
/// <b>Paged, unlike the matrix report and the trial balance.</b> Those two are aggregates, where a
/// partial answer is a wrong answer. This one is a list of individual vouchers, so a page is a
/// genuine subset — and the figures that must not be partial are returned separately, computed
/// over the whole filtered set (see <see cref="VoucherReviewResultDto"/>).
/// </para>
///
/// <para>
/// <b>Filters that are here, and one family that is not.</b> The reference project's screen also
/// offers کل/معین/تفصیلی bounds. Those are questions about a voucher's <i>lines</i>, and answering
/// them would change this report's meaning from "vouchers, with their totals" to "vouchers that
/// touch these accounts, with totals of only some lines" — two different reports. The line-level
/// view belongs in دفتر روزنامه, which filters by کد معین directly. They are left out rather than
/// approximated.
/// </para>
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetVoucherReviewQueryValidator.MaxPageSize"/>.</param>
/// <param name="Year">سال مالی — required exact match.</param>
/// <param name="FromVoucherNo">Optional inclusive lower bound on شماره سند.</param>
/// <param name="ToVoucherNo">Optional inclusive upper bound on شماره سند.</param>
/// <param name="FromDate">Optional inclusive lower bound on تاریخ سند (Jalali <c>YYYYMMDD</c>).</param>
/// <param name="ToDate">Optional inclusive upper bound on تاریخ سند.</param>
/// <param name="FromAtfNo">Optional inclusive lower bound on شماره عطف.</param>
/// <param name="ToAtfNo">Optional inclusive upper bound on شماره عطف.</param>
/// <param name="DocLife">Optional exact وضعیت سند filter.</param>
/// <param name="SystemTypeId">Optional exact نوع سند filter.</param>
/// <param name="Description">Optional «contains» filter on شرح سند.</param>
public sealed record GetVoucherReviewQuery(
    int PageNumber,
    int PageSize,
    string Year,
    string? FromVoucherNo = null,
    string? ToVoucherNo = null,
    string? FromDate = null,
    string? ToDate = null,
    string? FromAtfNo = null,
    string? ToAtfNo = null,
    int? DocLife = null,
    Guid? SystemTypeId = null,
    string? Description = null) : IRequest<VoucherReviewResultDto>, IVahedScopedQuery
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c> from the caller's effective unit — never bound
    /// from client input. The view carries <c>VAHEDCODE</c>, so without this the report would list
    /// every unit's vouchers.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
