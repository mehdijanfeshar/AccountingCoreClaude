namespace Accounting.Domain.ValueObjects;

/// <summary>
/// کدام عدد از حساب‌های یک ردیف <see cref="FsRowType.Account"/> خوانده شود —
/// <c>TB_FS_TEMPLATE_ROW.VALUE_TYPE</c>.
/// </summary>
public enum FsValueType
{
    /// <summary>مانده پایان دوره.</summary>
    Closing = 1,
    /// <summary>مانده ابتدای دوره.</summary>
    Opening = 2,
    /// <summary>گردش خالص دوره (بدهکار − بستانکار).</summary>
    Movement = 3,
    /// <summary>فقط گردش بدهکار دوره.</summary>
    Debit = 4,
    /// <summary>فقط گردش بستانکار دوره.</summary>
    Credit = 5,
}
