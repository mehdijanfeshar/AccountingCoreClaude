namespace Accounting.Application.Reports.CrossTab;

/// <summary>
/// A dimension the cross-tab can put on either axis — the same ten levels
/// <see cref="MatrixReport.MatrixReportLevel"/> offers, deliberately declared as its own type.
///
/// <para>
/// <b>Why a separate enum rather than reusing <c>MatrixReportLevel</c>.</b> There, a level is
/// "which level am I aggregating at" and the report has exactly one. Here it is "what goes on this
/// axis", and there are two independent choices. Sharing the type would couple two reports that
/// are free to grow apart — the moment this one gains نوع سند or کاربر as an axis (both are
/// columns on the same view), <c>MatrixReportLevel</c> would inherit a value that is meaningless
/// as a drill-down level. The numeric values are kept identical so the two are trivially
/// comparable when reading logs.
/// </para>
///
/// <para>
/// All ten map to plain columns of <c>VW_CONSOLIDATE_REPORT</c> — the view has already flattened
/// the account hierarchy and all seven تفصیلی levels onto every line. That flattening is the whole
/// reason a pivot across any pair of them is one <c>GROUP BY</c> with no joins.
/// </para>
/// </summary>
public enum CrossTabDimension
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
