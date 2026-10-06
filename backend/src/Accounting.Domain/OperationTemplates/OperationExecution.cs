namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// ردپای هر اجرای الگو: «این سند از کجا آمد؟»
/// ClientRequestId یکتا است تا دوبار کلیک روی «ثبت» دو سند نسازد (Idempotency).
/// در فاز Agent، شناسهٔ گفتگو و جملهٔ اصلی کاربر هم به این جدول اضافه می‌شود.
/// </summary>
public class OperationExecution
{
    public Guid Id { get; set; }
    public Guid ClientRequestId { get; set; }
    /// <summary>null = سند کامل/آزاد بدون الگو</summary>
    public Guid? OperationTemplateId { get; set; }
    /// <summary>کد الگو، یا <c>MANUAL</c> برای سند آزاد</summary>
    public string TemplateCode { get; set; } = default!;

    /// <summary>واحد سند — همیشه از توکن/هدر، هرگز از بدنه</summary>
    public string VahedCode { get; set; } = default!;

    /// <summary>ورودی نرمال‌شده به صورت JSON</summary>
    public string InputJson { get; set; } = default!;

    /// <summary><c>TB_VOUCHERSHEAD.ID</c></summary>
    public Guid VoucherId { get; set; }
    public string VoucherNo { get; set; } = default!;

    /// <summary>Form = اجرای مستقیم الگو، Compose = سند کامل ویرایش‌شده، Agent = گفتگو (فاز بعد)</summary>
    public string Channel { get; set; } = "Form";

    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}
