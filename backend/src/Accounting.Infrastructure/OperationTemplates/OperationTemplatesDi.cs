using Microsoft.Extensions.Configuration;
using Accounting.Application.OperationTemplates;
using Accounting.Application.OperationTemplates.History;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.OperationTemplates;

public static class OperationTemplatesDi
{
    /// <summary>ماژول Agent-UX فاز ۱ (الگوی عملیات). از Program.cs صدا زده می‌شود.</summary>
    public static IServiceCollection AddOperationTemplates(this IServiceCollection s)
    {
        s.AddScoped<IOperationTemplateRepository, OperationTemplateRepository>();
        s.AddScoped<IVoucherGenerationEngine, VoucherGenerationEngine>();
        s.AddScoped<TemplateDefinitionValidator>();
        s.AddScoped<ComposedVoucherBuilder>();
        s.AddScoped<AssistantUnitPolicy>();
        s.AddScoped<OperationRiskEvaluator>(); // فاز ۶۲ — ارزیاب قطعی ریسک (Preview و Execute)
        s.AddScoped<ISavedReportRepository, SavedReportRepository>();
        s.AddScoped<IQuickVoucherRepository, QuickVoucherRepository>();
        s.AddScoped<IHesabyarAuditRepository, HesabyarAuditRepository>(); // DDL 081/083
        s.AddScoped<IHesabyarVoucherStateHook, HesabyarVoucherStateHook>();
        s.AddScoped<ISubsidiaryAccountReader, SubsidiaryAccountReader>();
        s.AddScoped<IDetailReader, DetailReader>();
        s.AddScoped<IVoucherWriter, VoucherWriter>();
        // فاز ۶۳ — یادگیری از سوابق (DDL 085)
        s.AddScoped<IHistorySourceReader, HistorySourceReader>();
        s.AddScoped<IHistoryPatternStore, HistoryPatternRepository>();
        s.AddScoped<HistoryTemplateFactory>();
        s.AddScoped<HistoryOperationPlanner>();
        return s;
    }

    /// <summary>آستانه‌های ریسک حسابیار از <c>Assistant:Risk</c> (AnomalyFactor، LookbackMonths، MinSamples).</summary>
    public static IServiceCollection AddAssistantRisk(this IServiceCollection s, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        s.AddSingleton(configuration.GetSection(AssistantRiskOptions.Section).Get<AssistantRiskOptions>() ?? new AssistantRiskOptions());
        return s;
    }
}
