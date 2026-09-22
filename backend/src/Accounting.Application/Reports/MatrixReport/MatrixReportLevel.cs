namespace Accounting.Application.Reports.MatrixReport;

/// <summary>
/// Which level گزارش ماتریسی aggregates by — the report's single most important input.
///
/// <para>
/// Mirrors the reference project's <c>typeShow</c> (1..10) exactly, including the ordering, so the
/// two systems produce comparable output for the same selection. The first three come from the
/// account coding hierarchy; the last seven are the تفصیلی levels, which the view has already
/// flattened into <c>TAFSILICODE1..7</c> — that flattening is why one report can pivot across all
/// ten without a different join per level.
/// </para>
/// </summary>
public enum MatrixReportLevel
{
    /// <summary>گروه.</summary>
    Group = 1,

    /// <summary>کل.</summary>
    Kol = 2,

    /// <summary>معین.</summary>
    Moin = 3,

    /// <summary>تفصیلی سطح ۱.</summary>
    Tafsili1 = 4,

    /// <summary>تفصیلی سطح ۲.</summary>
    Tafsili2 = 5,

    /// <summary>تفصیلی سطح ۳.</summary>
    Tafsili3 = 6,

    /// <summary>تفصیلی سطح ۴.</summary>
    Tafsili4 = 7,

    /// <summary>تفصیلی سطح ۵.</summary>
    Tafsili5 = 8,

    /// <summary>تفصیلی سطح ۶.</summary>
    Tafsili6 = 9,

    /// <summary>تفصیلی سطح ۷.</summary>
    Tafsili7 = 10,
}
