using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure;
using Accounting.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure.Tests;

/// <summary>
/// Guards the one failure mode that every other test in this solution is blind to: a repository
/// interface that exists, compiles, is injected into a handler and is fully unit-tested with Moq,
/// but was never registered in <c>DependencyInjection.AddInfrastructure</c>. Nothing else catches
/// that — the Application tests all supply mocks, and the Api tests construct controllers directly
/// — so the first sign of a missing registration would be a runtime failure on a live endpoint.
///
/// This asserts against the <see cref="ServiceDescriptor"/> entries rather than resolving the
/// services, which is deliberate: resolving a repository would construct
/// <c>LegacyDbContext</c>, and this test suite must never risk touching a live Oracle database.
/// Inspecting the descriptors performs no I/O and no instantiation at all.
/// </summary>
public sealed class RepositoryRegistrationTests
{
    private static IServiceCollection BuildRegisteredServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        return services;
    }

    /// <summary>
    /// Every write-side repository interface must map to its concrete implementation. The phase-13
    /// (batch 2) entities are listed alongside the pre-existing ones so a future refactor that drops
    /// a registration is caught regardless of which entity it belongs to.
    /// </summary>
    [Theory]
    // Pre-existing (phases 5-11).
    [InlineData(typeof(IUnitOfWork))]
    [InlineData(typeof(IAccountCodeRepository))]
    [InlineData(typeof(IVoucherHeadRepository))]
    [InlineData(typeof(IVoucherDetailRepository))]
    [InlineData(typeof(IAccountCodeReadRepository))]
    [InlineData(typeof(IVoucherHeadReadRepository))]
    [InlineData(typeof(IVoucherDetailReadRepository))]
    // Phase 13 batch 2 — write side.
    [InlineData(typeof(IAccountCodeInterfaceRepository))]
    [InlineData(typeof(IAccountExceptionRepository))]
    [InlineData(typeof(IBillLogRepository))]
    [InlineData(typeof(IPersonActionRepository))]
    [InlineData(typeof(IPreDescribRepository))]
    [InlineData(typeof(IRabetRepository))]
    [InlineData(typeof(IWhiteAndBlackListRepository))]
    [InlineData(typeof(IWhiteListRepository))]
    // Phase 13 batch 2 — read side.
    [InlineData(typeof(IAccountCodeInterfaceReadRepository))]
    [InlineData(typeof(IAccountExceptionReadRepository))]
    [InlineData(typeof(IBillLogReadRepository))]
    [InlineData(typeof(IPersonActionReadRepository))]
    [InlineData(typeof(IPreDescribReadRepository))]
    [InlineData(typeof(IRabetReadRepository))]
    [InlineData(typeof(IWhiteAndBlackListReadRepository))]
    [InlineData(typeof(IWhiteListReadRepository))]
    public void AddInfrastructure_RegistersServiceAsScoped(Type serviceType)
    {
        var services = BuildRegisteredServices();

        var descriptor = services.SingleOrDefault(d => d.ServiceType == serviceType);

        Assert.True(
            descriptor is not null,
            $"{serviceType.Name} is not registered in AddInfrastructure. Any handler depending on "
                + "it will fail to resolve at runtime even though its unit tests (which use mocks) pass.");

        // Scoped is required: these all share the request-scoped LegacyDbContext / change tracker,
        // so a singleton would leak tracked entities across requests and a transient would break
        // the "handler owns the single transaction boundary" contract.
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    /// <summary>
    /// Pins the concrete type behind each phase-13 repository interface, so a copy/paste slip that
    /// registers (say) <c>IWhiteListRepository</c> against <c>WhiteAndBlackListRepository</c> is
    /// caught. Those two are the likeliest pair to be confused in this batch.
    /// </summary>
    [Theory]
    [InlineData(typeof(IAccountCodeInterfaceRepository), typeof(AccountCodeInterfaceRepository))]
    [InlineData(typeof(IAccountExceptionRepository), typeof(AccountExceptionRepository))]
    [InlineData(typeof(IBillLogRepository), typeof(BillLogRepository))]
    [InlineData(typeof(IPersonActionRepository), typeof(PersonActionRepository))]
    [InlineData(typeof(IPreDescribRepository), typeof(PreDescribRepository))]
    [InlineData(typeof(IRabetRepository), typeof(RabetRepository))]
    [InlineData(typeof(IWhiteAndBlackListRepository), typeof(WhiteAndBlackListRepository))]
    [InlineData(typeof(IWhiteListRepository), typeof(WhiteListRepository))]
    [InlineData(typeof(IAccountCodeInterfaceReadRepository), typeof(AccountCodeInterfaceReadRepository))]
    [InlineData(typeof(IAccountExceptionReadRepository), typeof(AccountExceptionReadRepository))]
    [InlineData(typeof(IBillLogReadRepository), typeof(BillLogReadRepository))]
    [InlineData(typeof(IPersonActionReadRepository), typeof(PersonActionReadRepository))]
    [InlineData(typeof(IPreDescribReadRepository), typeof(PreDescribReadRepository))]
    [InlineData(typeof(IRabetReadRepository), typeof(RabetReadRepository))]
    [InlineData(typeof(IWhiteAndBlackListReadRepository), typeof(WhiteAndBlackListReadRepository))]
    [InlineData(typeof(IWhiteListReadRepository), typeof(WhiteListReadRepository))]
    public void AddInfrastructure_MapsPhase13InterfaceToItsOwnImplementation(
        Type serviceType,
        Type expectedImplementationType)
    {
        var services = BuildRegisteredServices();

        var descriptor = services.Single(d => d.ServiceType == serviceType);

        Assert.Equal(expectedImplementationType, descriptor.ImplementationType);
    }
}
