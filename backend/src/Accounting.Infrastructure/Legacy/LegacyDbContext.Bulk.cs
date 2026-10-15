namespace Accounting.Infrastructure.Legacy;

public partial class LegacyDbContext
{
    /// <summary>
    /// اگر پر باشد، <see cref="AuditLogInterceptor"/> برای هر ذخیره فقط یک ردیف خلاصه (تعداد به تفکیک جدول) با همین
    /// برچسب می‌نویسد، نه یک ردیف برای هر رکورد — برای عملیات انبوهی مثل انتقال اسناد از سیستم قدیم.
    /// </summary>
    public string? BulkAuditLabel { get; set; }
}
