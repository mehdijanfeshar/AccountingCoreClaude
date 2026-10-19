namespace Accounting.Domain.OperationTemplates;

public enum ReviewAction
{
    /// <summary>بررسی شد — سند به «بررسی‌شده» رفت.</summary>
    Approved = 1,

    /// <summary>برگشت به ثبت‌کننده با دلیل — سند به «یادداشت» برگشت.</summary>
    Returned = 2,

    /// <summary>ثبت‌کننده پس از اصلاح، سند را دوباره «موقت» کرد.</summary>
    Resubmitted = 3,

    /// <summary>ثبت‌کننده به پرسش/برگشت حسابدار پاسخ نوشت (DDL 084)؛ وضعیت سند تغییر نمی‌کند.</summary>
    Replied = 4,
}

/// <summary>
/// تاریخچهٔ بررسی سند حسابیار (DDL 081): هر «بررسی شد» / «برگشت با دلیل» / «ارسال دوباره» یک ردیف.
/// وضعیت فعلی سند همیشه از <c>TB_VOUCHERSHEAD.DOCLIFE</c> می‌آید؛ این جدول فقط تاریخچه و پیام برگشت است.
/// </summary>
public class OperationReview
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public Guid VoucherId { get; set; }
    public ReviewAction Action { get; set; }

    /// <summary>کد دلیل برگشت (متن کوتاه فرانت)؛ برای Approved/Resubmitted null.</summary>
    public string? ReasonCode { get; set; }

    /// <summary>توضیح بررسی‌کننده برای ثبت‌کننده.</summary>
    public string? Message { get; set; }

    /// <summary>کاربری که عمل را انجام داد (کد ملی، همان هویت ADDUSERID).</summary>
    public string ReviewerId { get; set; } = default!;

    public string VahedCode { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}
