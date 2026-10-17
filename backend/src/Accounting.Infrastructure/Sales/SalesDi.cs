using Accounting.Application.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Sales;

public static class SalesDi
{
    /// <summary>ماژول فروش، فاز ۱ (DDL 080). از Program.cs صدا زده می‌شود؛ به AddOperationTemplates (IVoucherWriter و خواننده‌ها) وابسته است.</summary>
    public static IServiceCollection AddSales(this IServiceCollection s)
    {
        s.AddScoped<ISalesRepository, SalesRepository>();
        s.AddScoped<ISalesLedgerReader, SalesLedgerReader>();
        s.AddScoped<SalesAccess>();
        s.AddScoped<SalesDocEngine>();
        s.AddScoped<SalesDocBuilder>();
        return s;
    }
}
