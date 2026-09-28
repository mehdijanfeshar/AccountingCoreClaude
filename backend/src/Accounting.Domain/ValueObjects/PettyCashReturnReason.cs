namespace Accounting.Domain.ValueObjects;

/// <summary>
/// دلیل(های) برگشت یک صورت‌هزینهٔ تنخواه از «در انتظار بررسی» به «برگشتی» —
/// <c>TB_PC_DOC_EVENT.RETURN_REASONS</c> (کدها با <c>,</c> جدا). Enum ثابت، نه جدول قابل‌تنظیم.
/// عبارت‌ها دقیقاً از صفحهٔ ۸ پاورپوینت مرجع («برگشت سند برای اصلاح») گرفته شده‌اند.
/// </summary>
public enum PettyCashReturnReason
{
    /// <summary>پیوست ناقص است.</summary>
    AttachmentIncomplete = 1,

    /// <summary>حساب هزینه نادرست است.</summary>
    ExpenseAccountIncorrect = 2,

    /// <summary>مبلغ با مدرک مطابقت ندارد.</summary>
    AmountMismatch = 3,

    /// <summary>شرح هزینه نیازمند توضیح است.</summary>
    DescriptionNeedsClarification = 4,

    /// <summary>سایر.</summary>
    Other = 5,
}
