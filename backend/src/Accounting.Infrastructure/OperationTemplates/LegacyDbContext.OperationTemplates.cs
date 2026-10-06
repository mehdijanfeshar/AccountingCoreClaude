using Accounting.Infrastructure.OperationTemplates;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Legacy;

// Agent-UX: جدول‌های ماژول الگوی عملیات از طریق قلاب OnModelCreatingPartial ثبت می‌شوند
// تا فایل بزرگ LegacyDbContext.cs دست نخورد.
public partial class LegacyDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OperationTemplateConfig());
        modelBuilder.ApplyConfiguration(new TemplateParameterConfig());
        modelBuilder.ApplyConfiguration(new TemplateLineConfig());
        modelBuilder.ApplyConfiguration(new TemplateLineDetailConfig());
        modelBuilder.ApplyConfiguration(new OperationExecutionConfig());
        modelBuilder.ApplyConfiguration(new SavedReportConfig()); // DDL 072
    }
}
