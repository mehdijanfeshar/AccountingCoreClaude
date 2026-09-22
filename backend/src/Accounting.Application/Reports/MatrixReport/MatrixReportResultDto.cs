namespace Accounting.Application.Reports.MatrixReport;

/// <summary>
/// One resolved step of the drill-down path, with the name the UI needs for its breadcrumb.
/// </summary>
/// <param name="Level">The level this step pins.</param>
/// <param name="LevelLabel">Persian label of that level.</param>
/// <param name="Code">The code the caller drilled into.</param>
/// <param name="Name">
/// Its name, resolved server-side. Empty when the scope matches no lines at all — the caller's
/// path is echoed back regardless, so a mistyped code shows as «کد بدون نام» rather than silently
/// vanishing from the breadcrumb.
/// </param>
public sealed record MatrixReportScopeDto(
    MatrixReportLevel Level,
    string LevelLabel,
    string Code,
    string Name);

/// <summary>
/// The full answer for گزارش ماتریسی: the rows at the requested level, the path that led there,
/// and which levels are worth offering next.
///
/// <para>
/// Still unpaged, for the same reason as before: these are aggregates, and a partial aggregate is
/// not a smaller answer but a wrong one. Paging over them is the client's business — it can slice
/// rows it already holds without any total ever being computed from a slice.
/// </para>
/// </summary>
/// <param name="Rows">One row per distinct code at <paramref name="Level"/>, ordered by code.</param>
/// <param name="Level">The level these rows were grouped at.</param>
/// <param name="LevelLabel">Persian label of that level.</param>
/// <param name="Scope">The drill-down path, shallowest first, with names resolved.</param>
/// <param name="AvailableLevels">
/// Levels that actually carry data inside the current scope, deepest question first answered:
/// «کدام سطوح؟». A معین whose lines were never assigned a تفصیلی ۵ should not offer تفصیلی ۵ as a
/// destination — offering every level unconditionally is what makes a drill-down feel like a maze
/// of empty tables.
/// </param>
public sealed record MatrixReportResultDto(
    IReadOnlyList<MatrixReportRowDto> Rows,
    MatrixReportLevel Level,
    string LevelLabel,
    IReadOnlyList<MatrixReportScopeDto> Scope,
    IReadOnlyList<MatrixReportLevel> AvailableLevels);
