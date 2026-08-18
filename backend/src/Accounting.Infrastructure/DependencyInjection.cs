using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="LegacyDbContext"/> against Oracle using the
    /// <c>DefaultConnection</c> connection string (sourced from User Secrets in
    /// Development — never hardcoded here), plus <see cref="IUnitOfWork"/> and all write-side
    /// repositories, scoped to the request/use-case lifetime.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LegacyDbContext>(options =>
            options.UseOracle(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountCodeRepository, AccountCodeRepository>();
        services.AddScoped<IVoucherHeadRepository, VoucherHeadRepository>();

        return services;
    }
}
