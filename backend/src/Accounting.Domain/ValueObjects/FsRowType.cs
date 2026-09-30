namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع ردیف قالب — <c>TB_FS_TEMPLATE_ROW.ROW_TYPE</c> (سند منبع §۷-۱).
/// </summary>
public enum FsRowType
{
    /// <summary>عنوان بخش؛ بدون مقدار.</summary>
    Header = 1,
    /// <summary>جمع مانده/گردش حساب‌های انتخاب‌شده با <c>SELECTOR</c>.</summary>
    Account = 2,
    /// <summary>محاسبه از ردیف‌های دیگر با <c>FORMULA</c>.</summary>
    Formula = 3,
    /// <summary>مقدار دستی یا ورودی از سیستم دیگر (مثلاً ارزش فعلی تعهدات اکچوئری).</summary>
    External = 4,
    /// <summary>متن توضیحی بدون مقدار.</summary>
    Text = 5,
    /// <summary>ردیف خالی (فاصله).</summary>
    Blank = 6,
}
