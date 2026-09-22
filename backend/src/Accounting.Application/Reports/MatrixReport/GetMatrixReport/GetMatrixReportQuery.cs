using Accounting.Application.Common.Security;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Reports.MatrixReport.GetMatrixReport;

/// <summary>
/// گزارش ماتریسی (تلفیقی) — voucher activity aggregated at a chosen level of the coding or تفصیلی
/// hierarchy, optionally narrowed to a path through the levels above it. READ-ONLY; reads the
/// Oracle view <c>VW_CONSOLIDATE_REPORT</c>.
///
/// <para>
/// <b><see cref="Scope"/> is what makes this report different from تراز آزمایشی.</b> Without it,
/// "group by معین" is a flat list the trial balance already produces at its own levels. With it,
/// the same query answers «معین‌های داخل این کل» — so the report is navigated from کل to جزء by
/// appending a step, and from جزء to کل by dropping one. The view carries every level's code on
/// each line, so a step is one equality filter and depth costs nothing.
/// </para>
///
/// <para>
/// Not paged: a partial aggregate is not a smaller answer, it is a wrong one — its totals would
/// not add up to anything real. The client may slice rows it already holds.
/// </para>
/// </summary>
/// <param name="Year">سال مالی — required exact match.</param>
/// <param name="Level">Which level to group by. Must be deeper than every step of <paramref name="Scope"/>.</param>
/// <param name="Scope">
/// Ordered drill-down path, shallowest first. Empty means the whole unit — the top of the report.
/// </param>
/// <param name="FromDate">Optional Jalali <c>YYYYMMDD</c> lower bound on تاریخ سند (inclusive).</param>
/// <param name="ToDate">Optional Jalali <c>YYYYMMDD</c> upper bound on تاریخ سند (inclusive).</param>
/// <param name="FromVoucherNo">Optional lower bound on شماره سند (inclusive).</param>
/// <param name="ToVoucherNo">Optional upper bound on شماره سند (inclusive).</param>
/// <param name="DocLife">Optional exact وضعیت سند filter.</param>
/// <param name="SystemTypeId">Optional نوع سند filter.</param>
public sealed record GetMatrixReportQuery(
    string Year,
    MatrixReportLevel Level,
    IReadOnlyList<MatrixReportScopeItem>? Scope,
    string? FromDate,
    string? ToDate,
    string? FromVoucherNo,
    string? ToVoucherNo,
    int? DocLife,
    Guid? SystemTypeId) : IRequest<MatrixReportResultDto>, IVahedScopedQuery
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c> from the caller's effective unit — never bound
    /// from client input. The view carries <c>VAHEDCODE</c>, so without this the report would
    /// expose every unit's figures at once.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
