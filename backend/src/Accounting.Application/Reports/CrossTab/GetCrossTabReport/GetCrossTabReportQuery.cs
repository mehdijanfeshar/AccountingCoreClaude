using Accounting.Application.Common.Security;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Reports.CrossTab.GetCrossTabReport;

/// <summary>
/// گزارش متقاطع — voucher turnover aggregated across <b>two</b> dimensions at once: one on the
/// rows, one on the columns, with بدهکار/بستانکار in each intersection. READ-ONLY; reads the
/// Oracle view <c>VW_CONSOLIDATE_REPORT</c>.
///
/// <para>
/// <b>How this differs from گزارش ماتریسی, which it does not replace.</b> That report groups by
/// <i>one</i> level and is navigated from کل to جزء; its output is a list. This one crosses two
/// dimensions and its output is a grid, so it answers a question the other cannot ask at all —
/// «این تفصیلی در کدام معین‌ها گردش داشته، و چقدر؟». Both were kept because both are useful; the
/// project owner asked for this as a new report rather than a change to the existing one.
/// </para>
///
/// <para>
/// <b>Working-rule #2 is satisfied by construction, not by exception.</b> Every dimension here is
/// already a column of <c>VW_CONSOLIDATE_REPORT</c> — the view flattens the account hierarchy and
/// all seven تفصیلی levels onto each line — so the whole pivot is one GROUP BY over a read model,
/// with no join and no raw SQL. The two documented exceptions (phases 18 and 41) do not extend
/// here and must not be cited as precedent for it.
/// </para>
///
/// <para>
/// Not paged, for the same reason as گزارش ماتریسی: half of an aggregate is not a smaller answer,
/// it is a wrong one. Column <i>count</i> is capped instead — see
/// <see cref="GetCrossTabReportQueryValidator.MaxColumns"/> — because the column set is
/// data-dependent and an unbounded pivot can produce a grid no client can render.
/// </para>
/// </summary>
/// <param name="Year">سال مالی — required exact match.</param>
/// <param name="RowDimension">What the rows are.</param>
/// <param name="ColumnDimension">What the columns are. Must differ from <paramref name="RowDimension"/>.</param>
/// <param name="FromDate">Optional Jalali <c>YYYYMMDD</c> lower bound on تاریخ سند (inclusive).</param>
/// <param name="ToDate">Optional Jalali <c>YYYYMMDD</c> upper bound on تاریخ سند (inclusive).</param>
/// <param name="DocLife">Optional exact وضعیت سند filter.</param>
/// <param name="SystemTypeId">Optional نوع سند filter.</param>
/// <param name="RowCodeFilter">
/// Optional «شروع با» narrowing on the row dimension's code — the row half of the reference
/// screen's «فیلتر سطر».
/// </param>
/// <param name="ColumnCodeFilter">
/// Optional «شروع با» narrowing on the column dimension's code — the «فیلتر ستون» half. This is
/// also the practical answer to a pivot that is too wide: narrow the columns rather than raise the
/// cap.
/// </param>
public sealed record GetCrossTabReportQuery(
    string Year,
    CrossTabDimension RowDimension,
    CrossTabDimension ColumnDimension,
    string? FromDate,
    string? ToDate,
    int? DocLife,
    Guid? SystemTypeId,
    string? RowCodeFilter,
    string? ColumnCodeFilter) : IRequest<CrossTabResultDto>, IVahedScopedQuery
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c> from the caller's effective unit — never bound
    /// from client input. The view carries <c>VAHEDCODE</c>, so without this the report would
    /// expose every unit's figures at once.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
