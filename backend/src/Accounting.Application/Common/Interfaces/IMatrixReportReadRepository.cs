using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read side of گزارش ماتریسی. Backed by the Oracle view <c>VW_CONSOLIDATE_REPORT</c>.
///
/// <para>
/// Like <see cref="IAccountReviewReadRepository"/>, this satisfies team working-rule #2 by
/// construction rather than by documented exception: every dimension the report can put on an axis
/// is already a column of that view, so the pivot is a GROUP BY over a read model with no join to
/// the write model and no raw SQL. The view was verified in phase 40 — it keeps one row per
/// voucher line and filters deleted vouchers in its own join.
/// </para>
/// </summary>
public interface IMatrixReportReadRepository
{
    /// <summary>
    /// Returns the cross-tab for the query's two dimensions: columns in code order, rows in code
    /// order with their populated cells only, plus grand totals over the whole filtered set.
    ///
    /// <para>
    /// Lines carrying no code on either chosen axis are excluded rather than grouped under an
    /// empty key — a تفصیلی level a line was never assigned at is absence, not a category. This
    /// matches <see cref="IAccountReviewReadRepository"/>'s rule, and matters more here: with two
    /// axes, an unfiltered null would create both a blank row and a blank column.
    /// </para>
    /// </summary>
    Task<MatrixResultDto> GetAsync(
        GetMatrixReportQuery query,
        CancellationToken cancellationToken = default);
}
