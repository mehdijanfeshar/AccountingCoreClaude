namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// نمونهٔ طلایی (فاز ۶۵، DDL 088): جمله + الگو و جواب‌هایی که درست است. برای سنجش هوش مصنوعی پیش از روشن
/// کردن آن برای کاربران و پس از هر تغییر مدل/پرامپت.
/// </summary>
public class AiGoldenCase
{
    public Guid Id { get; set; }
    public string Sentence { get; set; } = default!;

    /// <summary>کد الگوی درست؛ null = هیچ الگویی نباید انتخاب شود.</summary>
    public string? ExpectedTemplateCode { get; set; }

    /// <summary>جواب‌های درست (JSON کلید ⇒ مقدار): مبلغ ریال، تاریخ شمسی yyyyMMdd، تفصیلی = نام، گزینه = مقدار.</summary>
    public string? ExpectedAnswersJson { get; set; }

    public string? Note { get; set; }

    /// <summary>اگر از یک اجرای واقعی ساخته شد.</summary>
    public Guid? SourceExecutionId { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

public enum AiEvalKind
{
    /// <summary>روی نمونه‌های طلایی.</summary>
    Golden = 1,

    /// <summary>اجرای سایه: جملهٔ اسناد واقعی که کاربر بدون هوش مصنوعی ثبت کرد، در برابر آنچه واقعاً ثبت شد.</summary>
    Shadow = 2,
}

/// <summary>یک دور سنجش (DDL 088).</summary>
public class AiEvalRun
{
    public Guid Id { get; set; }
    public AiEvalKind Kind { get; set; }
    public string VahedCode { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string? Model { get; set; }
    public string PromptVersion { get; set; } = default!;
    public int Total { get; set; }
    public int TemplateHits { get; set; }
    public int AnswerTotal { get; set; }
    public int AnswerHits { get; set; }
    public int Failures { get; set; }
    public int AvgLatencyMs { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>نتیجهٔ یک جمله در یک دور سنجش (DDL 088). جمله پوشانده ذخیره می‌شود.</summary>
public class AiEvalItem
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid? GoldenCaseId { get; set; }
    public Guid? ExecutionId { get; set; }
    public string MaskedSentence { get; set; } = default!;
    public string? ExpectedTemplateCode { get; set; }
    public string? ActualTemplateCode { get; set; }
    public bool TemplateOk { get; set; }
    public int AnswersExpected { get; set; }
    public int AnswersOk { get; set; }

    /// <summary>اختلاف‌ها (JSON فهرست {key, expected, actual}).</summary>
    public string? DiffJson { get; set; }

    public int LatencyMs { get; set; }
    public string? Error { get; set; }
}
