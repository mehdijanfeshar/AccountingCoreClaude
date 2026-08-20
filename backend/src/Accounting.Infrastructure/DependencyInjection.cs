using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Idp;
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
    /// and read-side repositories, scoped to the request/use-case lifetime, plus the outbound
    /// <see cref="ITokenManager"/> used to call other tamin services as a client.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LegacyDbContext>(options =>
            options.UseOracle(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountCodeRepository, AccountCodeRepository>();
        services.AddScoped<IVoucherHeadRepository, VoucherHeadRepository>();
        services.AddScoped<IVoucherDetailRepository, VoucherDetailRepository>();
        services.AddScoped<IAccountCodeReadRepository, AccountCodeReadRepository>();
        services.AddScoped<IVoucherHeadReadRepository, VoucherHeadReadRepository>();
        services.AddScoped<IVoucherDetailReadRepository, VoucherDetailReadRepository>();

        services.AddTaminTokenManager(config => PopulateTokenManagerConfiguration(config, configuration));

        return services;
    }

    /// <summary>
    /// Binds every child section under <c>Idp:*</c> (e.g. <c>Idp:tamin</c>) into a
    /// <see cref="TokenManagerDetail"/> keyed by the section name, matching the project owner's
    /// original config-key naming (<c>External_ClientId</c>, <c>External_ClientSecret</c>,
    /// <c>External_Resources</c>). Deliberately tolerant of missing/empty values here — see
    /// <c>appsettings.json</c>, whose <c>Idp:tamin</c> section ships with empty placeholders;
    /// real values live in User Secrets. Completeness is validated lazily by
    /// <see cref="TokenManager"/> on first use, not here at startup.
    /// </summary>
    private static void PopulateTokenManagerConfiguration(
        TokenManagerConfiguration tokenManagerConfiguration,
        IConfiguration configuration)
    {
        foreach (var idpSection in configuration.GetSection("Idp").GetChildren())
        {
            tokenManagerConfiguration.TokenManagers[idpSection.Key] = new TokenManagerDetail
            {
                Server = idpSection["Server"] ?? string.Empty,
                ClientId = idpSection["External_ClientId"] ?? string.Empty,
                ClientSecret = idpSection["External_ClientSecret"] ?? string.Empty,
                Audience = idpSection["Audience"],
                GrantType = idpSection["GrantType"] is { Length: > 0 } grantType ? grantType : "client_credentials",
                Resources = SplitResources(idpSection["External_Resources"]),
            };
        }
    }

    private static ICollection<string> SplitResources(string? rawResources)
    {
        if (string.IsNullOrWhiteSpace(rawResources))
        {
            return new List<string>();
        }

        return rawResources
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
