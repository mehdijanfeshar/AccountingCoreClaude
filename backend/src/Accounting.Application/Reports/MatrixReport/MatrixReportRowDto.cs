namespace Accounting.Application.Reports.MatrixReport;

/// <summary>
/// One aggregated row of گزارش ماتریسی.
/// </summary>
/// <param name="Code">The code at the selected level (گروه/کل/معین code, or تفصیلی code).</param>
/// <param name="Name">Its name.</param>
/// <param name="LevelLabel">Persian label of the level this row was grouped at — «گروه»/«کل»/«معین»/«تفصیلی».</param>
/// <param name="Debtor">مجموع بدهکار.</param>
/// <param name="Creditor">مجموع بستانکار.</param>
/// <param name="DebtorBalance">
/// ماندهٔ بدهکار — <c>max(debtor - creditor, 0)</c>. One-sided on purpose, matching the reference's
/// <c>GREATEST(...)</c>: exactly one of the two balance columns is non-zero for any row, which is
/// what makes the report readable as two columns rather than one signed number.
/// </param>
/// <param name="CreditorBalance">ماندهٔ بستانکار — <c>max(creditor - debtor, 0)</c>.</param>
/// <param name="HasChildren">
/// Whether drilling into this row would show anything: true when at least one line behind it
/// carries a code at the next level down.
///
/// <para>
/// Computed rather than assumed, because «the next level exists» and «this particular row has
/// anything at it» are different questions — a معین with no تفصیلی assignment is perfectly normal,
/// and offering a drill-down that lands on an empty table reads as a broken report.
/// </para>
/// </summary>
public sealed record MatrixReportRowDto(
    string Code,
    string Name,
    string LevelLabel,
    decimal Debtor,
    decimal Creditor,
    decimal DebtorBalance,
    decimal CreditorBalance,
    bool HasChildren);
