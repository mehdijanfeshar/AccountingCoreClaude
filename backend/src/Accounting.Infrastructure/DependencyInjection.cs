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

        // Phase 13 (batch 2) independent entities. Each follows the exact same write/read
        // repository split as the entities above: the write repository only stages changes and
        // never calls SaveChanges, while the read repository is AsNoTracking + DTO-projecting.
        // PreDescrib deliberately has no delete path at all — TB_PREDESCRIBS has no ISDELETED
        // column and this project never issues physical deletes; see PreDescribSchemaAssumptionsTests.
        services.AddScoped<IAccountCodeInterfaceRepository, AccountCodeInterfaceRepository>();
        services.AddScoped<IAccountExceptionRepository, AccountExceptionRepository>();
        services.AddScoped<IBillLogRepository, BillLogRepository>();
        services.AddScoped<IPersonActionRepository, PersonActionRepository>();
        services.AddScoped<IPreDescribRepository, PreDescribRepository>();
        services.AddScoped<IRabetRepository, RabetRepository>();
        services.AddScoped<IWhiteAndBlackListRepository, WhiteAndBlackListRepository>();
        services.AddScoped<IWhiteListRepository, WhiteListRepository>();

        services.AddScoped<IAccountCodeInterfaceReadRepository, AccountCodeInterfaceReadRepository>();
        services.AddScoped<IAccountExceptionReadRepository, AccountExceptionReadRepository>();
        services.AddScoped<IBillLogReadRepository, BillLogReadRepository>();
        services.AddScoped<IPersonActionReadRepository, PersonActionReadRepository>();
        services.AddScoped<IPreDescribReadRepository, PreDescribReadRepository>();
        services.AddScoped<IRabetReadRepository, RabetReadRepository>();
        services.AddScoped<IWhiteAndBlackListReadRepository, WhiteAndBlackListReadRepository>();
        services.AddScoped<IWhiteListReadRepository, WhiteListReadRepository>();

        // Phase 14 (batch 3) independent entities. Same write/read repository split again.
        // Two schema irregularities in this batch are worth knowing about when reading these
        // registrations, because they change the shape of the feature above the repository:
        //   - VahedInfo (TB_VAHED_INFO) has NO ISDELETED and NO audit columns whatsoever, so it
        //     gets Create/Read/Update only (no delete path) and its handlers do not depend on
        //     ICurrentUser at all — there is nowhere to stamp. See VahedInfoSchemaAssumptionsTests.
        //   - WorkShop (TB_WORKSHOP) has a NULLABLE ISDELETED (bool?), like TB_RABET, so both
        //     false and NULL mean "not deleted" throughout its handlers and read filters.
        services.AddScoped<IAttribForAccountCodeRepository, AttribForAccountCodeRepository>();
        services.AddScoped<IChequeTypeRepository, ChequeTypeRepository>();
        services.AddScoped<IIdentityGroupRepository, IdentityGroupRepository>();
        services.AddScoped<IIdentitySubGroupRepository, IdentitySubGroupRepository>();
        services.AddScoped<ILevelTafsilRepository, LevelTafsilRepository>();
        services.AddScoped<ITafsilGroupRepository, TafsilGroupRepository>();
        services.AddScoped<IVahedInfoRepository, VahedInfoRepository>();
        services.AddScoped<IWorkShopRepository, WorkShopRepository>();

        services.AddScoped<IAttribForAccountCodeReadRepository, AttribForAccountCodeReadRepository>();
        services.AddScoped<IChequeTypeReadRepository, ChequeTypeReadRepository>();
        services.AddScoped<IIdentityGroupReadRepository, IdentityGroupReadRepository>();
        services.AddScoped<IIdentitySubGroupReadRepository, IdentitySubGroupReadRepository>();
        services.AddScoped<ILevelTafsilReadRepository, LevelTafsilReadRepository>();
        services.AddScoped<ITafsilGroupReadRepository, TafsilGroupReadRepository>();
        services.AddScoped<IVahedInfoReadRepository, VahedInfoReadRepository>();
        services.AddScoped<IWorkShopReadRepository, WorkShopReadRepository>();

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
