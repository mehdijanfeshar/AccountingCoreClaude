namespace Accounting.Domain.ValueObjects;

/// <summary>
/// دلیل(های) برگشت یک صورت‌هزینهٔ تنخواه از «در انتظار بررسی» به «برگشتی» —
/// <c>TB_PC_DOC_EVENT.RETURN_REASONS</c> (کدها با <c>,</c> جدا). Enum ثابت، نه جدول قابل‌تنظیم —
/// هم‌الگو با بقیهٔ enumهای این ماژول (<c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲).
///
/// ⚠️ متن دقیق این عبارت‌ها از پاورپوینت مرجع استخراج نشده بود — این‌ها پیش‌نویس محافظه‌کارانه‌اند
/// و نیازمند تأیید صاحب پروژه‌اند؛ فقط مقادیر عددی (که در دادهٔ ذخیره‌شده اثر دائمی دارند) قطعی‌اند.
/// </summary>
public enum PettyCashReturnReason
{
    /// <summary>فاکتور/رسید ناقص.</summary>
    MissingInvoiceOrReceipt = 1,

    /// <summary>تاریخ فاکتور نامعتبر.</summary>
    InvoiceDateInvalid = 2,

    /// <summary>مغایرت مبلغ.</summary>
    AmountMismatch = 3,

    /// <summary>اطلاعات فروشنده ناقص.</summary>
    VendorInfoIncomplete = 4,

    /// <summary>مدارک پشتیبان ناقص.</summary>
    SupportingDocsMissing = 5,

    /// <summary>خارج از سقف/ضوابط تنخواه.</summary>
    ExceedsFundPolicy = 6,

    /// <summary>سایر.</summary>
    Other = 7,
}
