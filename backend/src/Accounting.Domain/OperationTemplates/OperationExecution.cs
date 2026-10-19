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

    /// <summary>تصویر JSON تعریف الگو در لحظهٔ اجرا (DDL 082)؛ null برای سند آزاد بدون الگو یا اجرای پیش از 082.</summary>
    public string? TemplateSnapshot { get; set; }

    /// <summary>SHA-256 (hex، ۶۴ نویسه) همان <see cref="TemplateSnapshot"/> (DDL 082).</summary>
    public string? TemplateHash { get; set; }

    /// <summary>ردپای هوش مصنوعی این اجرا — <see cref="OperationAiTrace"/> (DDL 083)؛ null = بدون هوش مصنوعی.</summary>
    public Guid? AiTraceId { get; set; }

    // ── DDL 084 ──

    /// <summary>جملهٔ کامل کاربر (تصمیم صاحب پروژه: در پایگاه خودمان کامل؛ فقط هنگام ارسال به هوش مصنوعی بیرونی پوشانده می‌شود).</summary>
    public string? UserSentence { get; set; }

    /// <summary>JSON منبع هر جواب: <c>{"amount":"sentence","customer":"ai"}</c> با مقدارهای sentence | ai | recent | quick | user.</summary>
    public string? AnswerSourcesJson { get; set; }

    /// <summary>کاربر جواب‌های پرشدهٔ هوش مصنوعی را صریحاً تأیید کرد؟</summary>
    public bool? AiConfirmed { get; set; }

    /// <summary>ریسک محاسبه‌شدهٔ لحظهٔ ثبت (همان ارزیاب Preview).</summary>
    public RiskLevel? RiskLevel { get; set; }

    /// <summary>JSON فهرست دلیل‌های ریسک.</summary>
    public string? RiskReasonsJson { get; set; }

    /// <summary>JSON فهرست هشدارهای نمایش‌داده‌شده.</summary>
    public string? WarningsJson { get; set; }

    /// <summary>جمع بدهکار سند (ریال) — مبنای میانهٔ ناهنجاری و تکرار محتوایی.</summary>
    public long? TotalAmount { get; set; }
}
