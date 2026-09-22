namespace Accounting.Application.Reports.MatrixReport;

/// <summary>
/// One step of the drill-down path — «در گروه ۱، در کل ۱۰، …». A request carries an ordered list
/// of these, and the report shows the rows of <c>Level</c> that live under all of them.
///
/// <para>
/// <b>This is what turns گزارش ماتریسی into a browsable report rather than a second تراز آزمایشی.</b>
/// Without a scope, choosing «کل» just lists every کل — which is genuinely what the trial balance
/// already does at its own levels. With a scope, «کل» means «the کل rows inside the گروه I opened»,
/// and the same mechanism read backwards is the way up.
/// </para>
///
/// <para>
/// The view has every level's code flattened onto each line, so a scope step is a plain equality
/// filter on one column — no joins, and the cost does not grow with depth.
/// </para>
/// </summary>
public sealed class MatrixReportScopeItem
{
    /// <summary>Which level this step pins. Must be shallower than the level being listed.</summary>
    public MatrixReportLevel Level { get; set; }

    /// <summary>The code at that level. Compared for exact equality, never as a pattern.</summary>
    public string Code { get; set; } = string.Empty;
}
