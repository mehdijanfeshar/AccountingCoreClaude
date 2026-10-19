namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// ردپای هر «فهم جمله» با هوش مصنوعی (DDL 083). <b>جملهٔ خام هرگز ذخیره نمی‌شود</b> — فقط نسخهٔ پوشانده‌شده
/// (کد ملی، شبا، کارت و تلفن با نشانه جایگزین). اجرا با <c>TB_OP_EXECUTION.AI_TRACE_ID</c> به آن وصل می‌شود.
/// </summary>
public class OperationAiTrace
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = default!;
    public string MaskedSentence { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string? Model { get; set; }
    public string PromptVersion { get; set; } = default!;

    /// <summary>پاسخ خام مدل (قبل از برگرداندن نشانه‌ها، پس بدون دادهٔ شخصی)؛ null در خطا.</summary>
    public string? ResultJson { get; set; }

    public double? Confidence { get; set; }
    public int LatencyMs { get; set; }
    public bool Succeeded { get; set; }

    /// <summary>خلاصهٔ خطا (نام نوع استثنا یا «timeout»)، بدون جزئیات سرویس بیرونی.</summary>
    public string? Error { get; set; }

    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}
