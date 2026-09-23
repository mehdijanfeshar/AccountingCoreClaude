namespace Accounting.Application.Reports.CrossTab;

/// <summary>
/// One column of the cross-tab — a distinct value of the column dimension.
///
/// <para>
/// The set of columns is <b>data-dependent</b>: it is whatever values actually occur inside the
/// filtered period, not a fixed schema. That is what makes this a pivot rather than a report with
/// columns someone chose in advance.
/// </para>
/// </summary>
/// <param name="Code">The dimension value's code — also the key cells refer to.</param>
/// <param name="Name">Its title, for the header.</param>
/// <param name="Debtor">Column total, بدهکار.</param>
/// <param name="Creditor">Column total, بستانکار.</param>
public sealed record CrossTabColumnDto(
    string Code,
    string Name,
    decimal Debtor,
    decimal Creditor);

/// <summary>
/// One cell — the intersection of a row and a column.
///
/// <para>
/// Only non-empty intersections are sent. A cross-tab is nearly always sparse (on live data, 8
/// rows × 8 columns held 15 populated cells out of 64), so materialising the full grid would spend
/// most of the payload on zeroes the client can infer from a missing key.
/// </para>
/// </summary>
/// <param name="ColumnCode">Which column this cell belongs to.</param>
/// <param name="Debtor">بدهکار at this intersection.</param>
/// <param name="Creditor">بستانکار at this intersection.</param>
public sealed record CrossTabCellDto(
    string ColumnCode,
    decimal Debtor,
    decimal Creditor);

/// <summary>
/// One row of the cross-tab — a distinct value of the row dimension, with its populated cells.
/// </summary>
/// <param name="Code">The dimension value's code.</param>
/// <param name="Name">Its title.</param>
/// <param name="Cells">Populated intersections only; absent column codes are zero.</param>
/// <param name="Debtor">Row total, بدهکار.</param>
/// <param name="Creditor">Row total, بستانکار.</param>
public sealed record CrossTabRowDto(
    string Code,
    string Name,
    IReadOnlyList<CrossTabCellDto> Cells,
    decimal Debtor,
    decimal Creditor);

/// <summary>
/// The whole cross-tab: which dimensions are on the axes, the columns that turned out to exist,
/// the rows, and the grand totals.
/// </summary>
/// <param name="RowDimension">What the rows are.</param>
/// <param name="RowDimensionLabel">Persian label for the row axis.</param>
/// <param name="ColumnDimension">What the columns are.</param>
/// <param name="ColumnDimensionLabel">Persian label for the column axis.</param>
/// <param name="Columns">Columns in code order, already truncated if there were too many.</param>
/// <param name="Rows">Rows in code order.</param>
/// <param name="Debtor">Grand total, بدهکار — over the <b>whole</b> filtered set.</param>
/// <param name="Creditor">Grand total, بستانکار — over the whole filtered set.</param>
/// <param name="TotalColumnCount">
/// How many distinct column values the filtered set actually had, before any cap.
/// </param>
/// <param name="ColumnsTruncated">
/// <see langword="true"/> when <paramref name="Columns"/> holds fewer than
/// <paramref name="TotalColumnCount"/>.
///
/// <para>
/// ⚠️ When this is set, the grand totals still cover everything while the visible cells do not —
/// the row and column totals will not add up to them. That is deliberate: silently re-basing the
/// totals onto the visible slice would make a truncated report look complete. The client must say
/// so on screen.
/// </para>
/// </param>
public sealed record CrossTabResultDto(
    CrossTabDimension RowDimension,
    string RowDimensionLabel,
    CrossTabDimension ColumnDimension,
    string ColumnDimensionLabel,
    IReadOnlyList<CrossTabColumnDto> Columns,
    IReadOnlyList<CrossTabRowDto> Rows,
    decimal Debtor,
    decimal Creditor,
    int TotalColumnCount,
    bool ColumnsTruncated);
