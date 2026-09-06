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
    // Phase 14 batch 3 — write side. VahedInfo appears here even though it has no delete path
    // (TB_VAHED_INFO has no ISDELETED column): a missing registration would break its Create and
    // Update endpoints just as badly, so it must be covered like any other write repository.
    [InlineData(typeof(IAttribForAccountCodeRepository))]
    [InlineData(typeof(IChequeTypeRepository))]
    [InlineData(typeof(IIdentityGroupRepository))]
    [InlineData(typeof(IIdentitySubGroupRepository))]
    [InlineData(typeof(ILevelTafsilRepository))]
    [InlineData(typeof(ITafsilGroupRepository))]
    [InlineData(typeof(IVahedInfoRepository))]
    [InlineData(typeof(IWorkShopRepository))]
    // Phase 14 batch 3 — read side.
    [InlineData(typeof(IAttribForAccountCodeReadRepository))]
    [InlineData(typeof(IChequeTypeReadRepository))]
    [InlineData(typeof(IIdentityGroupReadRepository))]
    [InlineData(typeof(IIdentitySubGroupReadRepository))]
    [InlineData(typeof(ILevelTafsilReadRepository))]
    [InlineData(typeof(ITafsilGroupReadRepository))]
    [InlineData(typeof(IVahedInfoReadRepository))]
    [InlineData(typeof(IWorkShopReadRepository))]
    // Phase 15 batch 4 — write side. Unlike batches 2 and 3, every entity here owns an ISDELETED
    // column, so all eight have a full Create/Update/Delete surface behind these registrations.
    [InlineData(typeof(IBankAccountRepository))]
    [InlineData(typeof(IBankCartDetailRepository))]
    [InlineData(typeof(ICheckBookRepository))]
    [InlineData(typeof(IChequesIncorrentRepository))]
    [InlineData(typeof(IElamHeadRepository))]
    [InlineData(typeof(IExpenseRepository))]
    [InlineData(typeof(IReceiptRepository))]
    [InlineData(typeof(IRevolvingFundRepository))]
    // Phase 15 batch 4 — read side.
    [InlineData(typeof(IBankAccountReadRepository))]
    [InlineData(typeof(IBankCartDetailReadRepository))]
    [InlineData(typeof(ICheckBookReadRepository))]
    [InlineData(typeof(IChequesIncorrentReadRepository))]
    [InlineData(typeof(IElamHeadReadRepository))]
    [InlineData(typeof(IExpenseReadRepository))]
    [InlineData(typeof(IReceiptReadRepository))]
    [InlineData(typeof(IRevolvingFundReadRepository))]
    // Phase 16 batch 5 — two Head tables, both with a full Create/Update/Delete surface behind
    // these registrations. Their Detail children are deliberately unregistered (and have no
    // repository at all) because the aggregate boundaries are undecided.
    [InlineData(typeof(IPayReciveHeadRepository))]
    [InlineData(typeof(ITmpVoucherHeadRepository))]
    [InlineData(typeof(IPayReciveHeadReadRepository))]
    [InlineData(typeof(ITmpVoucherHeadReadRepository))]
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
    /// Pins the concrete type behind each batched repository interface, so a copy/paste slip that
    /// registers (say) <c>IWhiteListRepository</c> against <c>WhiteAndBlackListRepository</c> is
    /// caught. Every batch of near-identical CRUD entities has at least one such confusable pair:
    /// in phase 13 it was <c>WhiteList</c>/<c>WhiteAndBlackList</c>; in phase 14 there are two,
    /// <c>IdentityGroup</c>/<c>IdentitySubGroup</c> and <c>LevelTafsil</c>/<c>TafsilGroup</c> —
    /// the latter pair being especially easy to transpose since both names contain "Tafsil".
    /// Note that <see cref="AddInfrastructure_RegistersServiceAsScoped"/> above would NOT catch a
    /// crossed registration: the descriptor still exists and is still scoped, it just points at
    /// the wrong table's repository, which would silently read and write the wrong Oracle table.
    ///
    /// Phase 15 introduces the most dangerous confusable pair in the whole project, so
    /// <see cref="IAccountCodeRepository"/> is pinned here too even though it predates the batched
    /// entities: <c>BankAccount</c> is <c>TB_ACCOUNT</c> (the bank account master — account number,
    /// IBAN, card number) while <c>AccountCode</c> is <c>TB_ACCOUNTCODE</c> (the chart-of-accounts
    /// node). Both are "account" repositories, they sit next to each other in
    /// <c>AddInfrastructure</c>, and a transposition between them would silently write bank-account
    /// rows into the chart of accounts — the single worst silent-corruption outcome available in
    /// this codebase. <c>CheckBook</c>/<c>ChequesIncorrent</c> is the batch's other confusable pair.
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
    // Phase 14 batch 3.
    [InlineData(typeof(IAttribForAccountCodeRepository), typeof(AttribForAccountCodeRepository))]
    [InlineData(typeof(IChequeTypeRepository), typeof(ChequeTypeRepository))]
    [InlineData(typeof(IIdentityGroupRepository), typeof(IdentityGroupRepository))]
    [InlineData(typeof(IIdentitySubGroupRepository), typeof(IdentitySubGroupRepository))]
    [InlineData(typeof(ILevelTafsilRepository), typeof(LevelTafsilRepository))]
    [InlineData(typeof(ITafsilGroupRepository), typeof(TafsilGroupRepository))]
    [InlineData(typeof(IVahedInfoRepository), typeof(VahedInfoRepository))]
    [InlineData(typeof(IWorkShopRepository), typeof(WorkShopRepository))]
    [InlineData(typeof(IAttribForAccountCodeReadRepository), typeof(AttribForAccountCodeReadRepository))]
    [InlineData(typeof(IChequeTypeReadRepository), typeof(ChequeTypeReadRepository))]
    [InlineData(typeof(IIdentityGroupReadRepository), typeof(IdentityGroupReadRepository))]
    [InlineData(typeof(IIdentitySubGroupReadRepository), typeof(IdentitySubGroupReadRepository))]
    [InlineData(typeof(ILevelTafsilReadRepository), typeof(LevelTafsilReadRepository))]
    [InlineData(typeof(ITafsilGroupReadRepository), typeof(TafsilGroupReadRepository))]
    [InlineData(typeof(IVahedInfoReadRepository), typeof(VahedInfoReadRepository))]
    [InlineData(typeof(IWorkShopReadRepository), typeof(WorkShopReadRepository))]
    // Phase 15 batch 4. IAccountCodeRepository/IAccountCodeReadRepository are pinned alongside the
    // BankAccount pair on purpose — see the XML doc above for why that transposition is the worst
    // one available in this codebase.
    [InlineData(typeof(IAccountCodeRepository), typeof(AccountCodeRepository))]
    [InlineData(typeof(IAccountCodeReadRepository), typeof(AccountCodeReadRepository))]
    [InlineData(typeof(IBankAccountRepository), typeof(BankAccountRepository))]
    [InlineData(typeof(IBankCartDetailRepository), typeof(BankCartDetailRepository))]
    [InlineData(typeof(ICheckBookRepository), typeof(CheckBookRepository))]
    [InlineData(typeof(IChequesIncorrentRepository), typeof(ChequesIncorrentRepository))]
    [InlineData(typeof(IElamHeadRepository), typeof(ElamHeadRepository))]
    [InlineData(typeof(IExpenseRepository), typeof(ExpenseRepository))]
    [InlineData(typeof(IReceiptRepository), typeof(ReceiptRepository))]
    [InlineData(typeof(IRevolvingFundRepository), typeof(RevolvingFundRepository))]
    [InlineData(typeof(IBankAccountReadRepository), typeof(BankAccountReadRepository))]
    [InlineData(typeof(IBankCartDetailReadRepository), typeof(BankCartDetailReadRepository))]
    [InlineData(typeof(ICheckBookReadRepository), typeof(CheckBookReadRepository))]
    [InlineData(typeof(IChequesIncorrentReadRepository), typeof(ChequesIncorrentReadRepository))]
    [InlineData(typeof(IElamHeadReadRepository), typeof(ElamHeadReadRepository))]
    [InlineData(typeof(IExpenseReadRepository), typeof(ExpenseReadRepository))]
    [InlineData(typeof(IReceiptReadRepository), typeof(ReceiptReadRepository))]
    [InlineData(typeof(IRevolvingFundReadRepository), typeof(RevolvingFundReadRepository))]
    // Phase 16 batch 5. The confusable pair here is TmpVoucherHead vs the pre-existing
    // VoucherHead: TB_TMP_VOUCHERHEAD is the *staging* table an imported document lands in, while
    // TB_VOUCHERSHEAD is the real accounting voucher. A transposition between them would either
    // write real vouchers into the staging table or — far worse — write unvalidated staged
    // documents straight into the live ledger, bypassing the voucher write path entirely. Both
    // sides of that pair are pinned here, exactly as AccountCode/BankAccount were in phase 15.
    [InlineData(typeof(IVoucherHeadRepository), typeof(VoucherHeadRepository))]
    [InlineData(typeof(IVoucherHeadReadRepository), typeof(VoucherHeadReadRepository))]
    [InlineData(typeof(IPayReciveHeadRepository), typeof(PayReciveHeadRepository))]
    [InlineData(typeof(ITmpVoucherHeadRepository), typeof(TmpVoucherHeadRepository))]
    [InlineData(typeof(IPayReciveHeadReadRepository), typeof(PayReciveHeadReadRepository))]
    [InlineData(typeof(ITmpVoucherHeadReadRepository), typeof(TmpVoucherHeadReadRepository))]
    public void AddInfrastructure_MapsInterfaceToItsOwnImplementation(
        Type serviceType,
        Type expectedImplementationType)
    {
        var services = BuildRegisteredServices();

        var descriptor = services.Single(d => d.ServiceType == serviceType);

        Assert.Equal(expectedImplementationType, descriptor.ImplementationType);
    }
}
