namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// سند پرتکرار حسابیار (DDL 079): یک الگوی عملیات + جواب‌های ثابت برای یک واحد. هنگام ثبت فقط
/// پارامترهای ثابت‌نشده (معمولاً مبلغ) پرسیده می‌شود و تاریخ سند همان روز است؛ سند را همان موتور الگو می‌سازد.
/// </summary>
public class QuickVoucher
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    public Guid TemplateId { get; set; }
    public string Title { get; set; } = "";
    /// <summary>JSON کلید پارامتر ⇒ مقدار (همان قالب Values در Execute؛ تاریخ‌ها ذخیره نمی‌شوند).</summary>
    public string ValuesJson { get; set; } = "{}";
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
