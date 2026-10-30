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
        modelBuilder.ApplyConfiguration(new QuickVoucherConfig()); // DDL 079
        modelBuilder.ApplyConfiguration(new OperationReviewConfig()); // DDL 081
        modelBuilder.ApplyConfiguration(new OperationAiTraceConfig()); // DDL 083
        modelBuilder.ApplyConfiguration(new OperationRequestConfig()); // DDL 084
        modelBuilder.ApplyConfiguration(new HistoryPatternConfig()); // DDL 085
        modelBuilder.ApplyConfiguration(new HistoryPatternUnitConfig()); // DDL 085
        modelBuilder.ApplyConfiguration(new HistoryDismissedConfig()); // DDL 086
        modelBuilder.ApplyConfiguration(new HistoryPatternVectorConfig()); // DDL 089
        modelBuilder.ApplyConfiguration(new HistoryPatternMeaningConfig()); // DDL 089
        modelBuilder.ApplyConfiguration(new AgentConversationConfig()); // DDL 087
        modelBuilder.ApplyConfiguration(new AgentMessageConfig()); // DDL 087
        modelBuilder.ApplyConfiguration(new AiGoldenCaseConfig()); // DDL 088
        modelBuilder.ApplyConfiguration(new AiEvalRunConfig()); // DDL 088
        modelBuilder.ApplyConfiguration(new AiEvalItemConfig()); // DDL 088
        Accounting.Infrastructure.Sales.SalesMappingExtensions.ApplySales(modelBuilder); // DDL 080
    }
}
