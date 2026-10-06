namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// گزارش ذخیره‌شدهٔ حسابیار (گزارش‌ساز): یکی از گزارش‌های موجود + تنظیمات ثابت + پارامترهایی که هر بار
/// پرسیده می‌شوند. فقط تعریف؛ اجرا با همان صفحهٔ گزارش موجود (DDL 072).
/// </summary>
public class SavedReport
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    /// <summary>کلمات کلیدی و جمله‌های نمونه، هر خط یکی (جستجوی حسابیار و هوش مصنوعی).</summary>
    public string? Keywords { get; set; }
    /// <summary><c>trial-balance</c> | <c>matrix</c> | <c>account-review</c> | <c>voucher-review</c></summary>
    public string ReportKind { get; set; } = "";
    /// <summary>JSON: <c>{"fixed":{"level":"2",...},"ask":["period",...],"period":"thisMonth"}</c></summary>
    public string SettingsJson { get; set; } = "{}";
    /// <summary>کدهای TB_VAHEDTYPE.TYPECODE با «,»؛ null = همه.</summary>
    public string? AllowedVahedTypes { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
