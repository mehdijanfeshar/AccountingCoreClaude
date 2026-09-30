using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Commands.Common;

/// <summary>
/// بدنهٔ ساخت/ویرایش/ورود یک ردیف قالب. والد با <b>کد</b> ردیف داده می‌شود (نه شناسه) تا ورود
/// دسته‌ای و seed هم با همین شکل کار کند. <paramref name="OrderNo"/> خالی = انتهای نسخه (افزودن)،
/// بدون تغییر (ویرایش)، یا ترتیب فهرست (ورود).
/// </summary>
public sealed record FsTemplateRowInput(
    string Code,
    FsRowType RowType,
    string? TitleFa,
    string? TitleEn = null,
    string? ParentCode = null,
    string? NoteRef = null,
    FsNormalBalance? NormalBalance = null,
    string? Selector = null,
    FsValueType? ValueType = null,
    string? Formula = null,
    FsRowFormat? Format = null,
    bool IsDrillable = true,
    bool AllowManualAdjust = false,
    int? OrderNo = null);
