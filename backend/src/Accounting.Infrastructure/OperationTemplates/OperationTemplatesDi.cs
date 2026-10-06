using Accounting.Application.OperationTemplates;
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
        s.AddScoped<ISavedReportRepository, SavedReportRepository>();
        s.AddScoped<ISubsidiaryAccountReader, SubsidiaryAccountReader>();
        s.AddScoped<IDetailReader, DetailReader>();
        s.AddScoped<IVoucherWriter, VoucherWriter>();
        return s;
    }
}
