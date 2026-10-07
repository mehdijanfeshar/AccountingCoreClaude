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
        var schema = configuration["Database:Schema"];

        if (!string.IsNullOrWhiteSpace(schema))
        {
            LegacyDbContext.DefaultSchema = schema.Trim().ToUpperInvariant();
        }

        services.AddDbContext<LegacyDbContext>(options =>
            options.UseOracle(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountCodeRepository, AccountCodeRepository>();
        services.AddScoped<IDeleteDependencyChecker, DeleteDependencyChecker>();
        services.AddScoped<IVoucherRestoreRepository, VoucherRestoreRepository>();
        services.AddScoped<ISystemVoucherInboxRepository, SystemVoucherInboxRepository>();
        services.AddScoped<Accounting.Application.Vouchers.Commands.Common.IAccountEntryPolicy, AccountEntryPolicy>();
        services.AddScoped<IMonthReopenRepository, MonthReopenRepository>();
        services.AddScoped<Accounting.Application.Common.Security.IRoleMenuAccessStore, RoleMenuAccessStore>();
        services.AddSingleton<IMonthReopenCodeGenerator, MonthReopenCodeGenerator>();
        services.AddScoped<IMonthCloseRepository, MonthCloseRepository>();
        services.AddScoped<IVoucherHeadRepository, VoucherHeadRepository>();
        services.AddScoped<IVoucherDetailRepository, VoucherDetailRepository>();
        services.AddScoped<IAccountCodeReadRepository, AccountCodeReadRepository>();
        services.AddScoped<IVoucherHeadReadRepository, VoucherHeadReadRepository>();
        services.AddScoped<ISysTypeReadRepository, SysTypeReadRepository>();
        services.AddScoped<IVoucherDetailReadRepository, VoucherDetailReadRepository>();

        // Phase 15 — read-only trial balance reporting (4/6/8-column). Single raw-SQL read
        // repository shared by all three report Queries; no write repository, no Command, no
        // entity mutation — see TrialBalanceReadRepository XML doc.
        services.AddScoped<ITrialBalanceReadRepository, TrialBalanceReadRepository>();
        services.AddScoped<Accounting.Application.Reports.GeneralLedger.IGeneralLedgerReadRepository, GeneralLedgerReadRepository>();

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
        services.AddScoped<IIdentityHeadRepository, IdentityHeadRepository>();
        services.AddScoped<IIdentitySubGroupRepository, IdentitySubGroupRepository>();
        services.AddScoped<ILevelTafsilRepository, LevelTafsilRepository>();
        services.AddScoped<ITafsilGroupRepository, TafsilGroupRepository>();
        services.AddScoped<IVahedInfoRepository, VahedInfoRepository>();
        services.AddScoped<IWorkShopRepository, WorkShopRepository>();

        services.AddScoped<IAttribForAccountCodeReadRepository, AttribForAccountCodeReadRepository>();
        services.AddScoped<IChequeTypeReadRepository, ChequeTypeReadRepository>();
        services.AddScoped<IIdentityGroupReadRepository, IdentityGroupReadRepository>();
        services.AddScoped<IIdentityHeadReadRepository, IdentityHeadReadRepository>();
        services.AddScoped<IIdentitySubGroupReadRepository, IdentitySubGroupReadRepository>();
        services.AddScoped<ILevelTafsilReadRepository, LevelTafsilReadRepository>();
        services.AddScoped<ITafsilGroupReadRepository, TafsilGroupReadRepository>();
        services.AddScoped<IVahedInfoReadRepository, VahedInfoReadRepository>();
        // Read-only by design — there is deliberately no IVahedTypeRepository counterpart; see
        // IVahedTypeReadRepository's XML doc.
        services.AddScoped<IVahedTypeReadRepository, VahedTypeReadRepository>();
        services.AddScoped<IUnitAccessReadRepository, UnitAccessReadRepository>();
        services.AddScoped<IYearReadRepository, YearReadRepository>();
        services.AddScoped<IAccountReviewReadRepository, AccountReviewReadRepository>();
        // Reads the same verified view as the matrix report, crossed on two axes instead of one.
        services.AddScoped<IMatrixReportReadRepository, MatrixReportReadRepository>();
        services.AddScoped<IVoucherReviewReadRepository, VoucherReviewReadRepository>();
        services.AddScoped<IAttributeAccountReconciliationReadRepository, AttributeAccountReconciliationReadRepository>();
        services.AddScoped<IElamWorkflowRepository, ElamWorkflowRepository>();
        // شناسه/ویژگی/فیش ردیف سند.
        services.AddScoped<Accounting.Application.Vouchers.Commands.Common.IVoucherLineExtrasStore, VoucherLineExtrasStore>();
        services.AddScoped<IElamWorkflowReadRepository, ElamWorkflowReadRepository>();
        services.AddScoped<IBankCardRepository, BankCardRepository>();
        services.AddScoped<IChequeBookRepository, ChequeBookRepository>();
        // اعلامیهٔ درآمد — وب‌سرویس SOAP سامانهٔ سبا (درآمد) (آدرس: ElamDrmd:Url).
        services.AddScoped<Accounting.Application.Elams.IRevenueElamSender, Accounting.Infrastructure.Services.ElamDrmdWebService>();
        services.AddScoped<IAccountJournalReadRepository, AccountJournalReadRepository>();
        services.AddScoped<IWorkShopReadRepository, WorkShopReadRepository>();

        // Phase 15 (batch 4) independent entities. Same write/read repository split again.
        // Unlike the two batches above, EVERY entity in this batch owns an ISDELETED column, so all
        // eight get a full CRUD surface — there is no CRU-only exception here (contrast PreDescrib
        // in phase 13 and VahedInfo in phase 14). Two things are worth knowing when reading these:
        //   - BankAccount is TB_ACCOUNT, the BANK account master — NOT the TB_ACCOUNTCODE
        //     chart-of-accounts node, whose repositories are IAccountCodeRepository above. The
        //     "BankAccount" prefix exists precisely so these two can never be confused at a call
        //     site; see AddInfrastructure_MapsInterfaceToItsOwnImplementation for the guard.
        //   - BankAccount, BankCartDetail, Expense, RevolvingFund and ElamHead all have a NULLABLE
        //     ISDELETED (bool?), so both false and NULL mean "not deleted" throughout their
        //     handlers and read filters; CheckBook, ChequesIncorrent and Receipt are non-nullable.
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<IBankCartDetailRepository, BankCartDetailRepository>();
        services.AddScoped<ICheckBookRepository, CheckBookRepository>();
        services.AddScoped<IChequesIncorrentRepository, ChequesIncorrentRepository>();
        services.AddScoped<IElamHeadRepository, ElamHeadRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IRevolvingFundRepository, RevolvingFundRepository>();

        services.AddScoped<IBankAccountReadRepository, BankAccountReadRepository>();
        services.AddScoped<IBankCartDetailReadRepository, BankCartDetailReadRepository>();
        services.AddScoped<ICheckBookReadRepository, CheckBookReadRepository>();
        services.AddScoped<IChequesIncorrentReadRepository, ChequesIncorrentReadRepository>();
        services.AddScoped<IElamHeadReadRepository, ElamHeadReadRepository>();
        services.AddScoped<IExpenseReadRepository, ExpenseReadRepository>();
        services.AddScoped<IReceiptReadRepository, ReceiptReadRepository>();
        services.AddScoped<IRevolvingFundReadRepository, RevolvingFundReadRepository>();

        // Phase 16 (batch 5) independent entities — a deliberately small batch of just two, both
        // of which are Head tables whose Detail children are explicitly OUT of scope because the
        // aggregate boundary for each pair is still undecided (see docs/open-decisions.md).
        // Both own an ISDELETED column so both get a full CRUD surface, but they differ in a way
        // that matters when reading their handlers:
        //   - PayReciveHead (TB_PAYRECIVHEAD) has a NON-nullable ISDELETED plus five NOT NULL
        //     business columns, so its validators carry NotEmpty rules and its handlers test
        //     `entity.ISDELETED` directly.
        //   - TmpVoucherHead (TB_TMP_VOUCHERHEAD) is the opposite extreme: every single column is
        //     nullable, including ISDELETED (bool?), so both false and NULL mean "not deleted"
        //     throughout its handlers and read filters, and its validators have no NotEmpty rules.
        // Neither table has any UNIQUE constraint, so neither controller declares 409.
        services.AddScoped<IPayReciveHeadRepository, PayReciveHeadRepository>();
        services.AddScoped<ITmpVoucherHeadRepository, TmpVoucherHeadRepository>();

        services.AddScoped<IPayReciveHeadReadRepository, PayReciveHeadReadRepository>();
        services.AddScoped<ITmpVoucherHeadReadRepository, TmpVoucherHeadReadRepository>();

        // Phase 20-b — read-only dynamic تفصیلی lookups for the voucher-entry form. No write
        // repository: TB_ACCOUNT_LINK_LEVEL and TB_TAFSIL_LINK_TAFSILGROUP are section-2 embedded
        // children (docs/tamin-core-entity-reference.md), never independent aggregates, and this
        // whole phase is read-only by design. See TafsiliLookupReadRepository XML doc.
        services.AddScoped<ITafsiliLookupReadRepository, TafsiliLookupReadRepository>();

        // New independent aggregate: Tafsili (TB_TAFSILI). Its گروه‌تفصیلی link
        // (TB_TAFSIL_LINK_TAFSILGROUP) stays embedded per the same team rule referenced above —
        // ITafsiliRepository exposes parent-scoped Get/Add methods for it, never an independent
        // repository/Command. See NoIndependentLinkTableWritePathTests.
        services.AddScoped<ITafsiliRepository, TafsiliRepository>();
        services.AddScoped<ITafsiliReadRepository, TafsiliReadRepository>();

        // Petty-cash module, chunk 1 (2026-09-27). IChargeAndCostRepository is the composite
        // write path for the Legacy TB_CHARGEANDCOST_HEAD/DETAIL pair — not an independent CRUD
        // surface, see that interface's XML doc. The TB_PC_* repositories are the module's own
        // "side" tables (owner-approved exception to "no new tables";
        // docs/tankhah-khazaneh-module.md §0/§3). IPettyCashFundRepository (2026-09-28) replaced
        // IPettyCashFundSettingRepository when TB_PC_FUND absorbed TB_REVOLVING_FUND/
        // TB_PC_FUND_SETTING for this module entirely.
        services.AddScoped<IChargeAndCostRepository, ChargeAndCostRepository>();
        services.AddScoped<IPettyCashFundRepository, PettyCashFundRepository>();
        services.AddScoped<IPettyCashExpenseDocRepository, PettyCashExpenseDocRepository>();
        services.AddScoped<IPettyCashDocEventRepository, PettyCashDocEventRepository>();

        services.AddScoped<IPettyCashFundReadRepository, PettyCashFundReadRepository>();
        services.AddScoped<IPettyCashExpenseDocReadRepository, PettyCashExpenseDocReadRepository>();
        services.AddScoped<IPettyCashDocEventReadRepository, PettyCashDocEventReadRepository>();

        // Petty-cash module, chunk 2 (بخش ۲) — RBAC side table (docs/tankhah-khazaneh-module.md,
        // تصمیم‌های بخش ۲).
        services.AddScoped<IPettyCashFundReviewerRepository, PettyCashFundReviewerRepository>();
        services.AddScoped<IPettyCashFundReviewerReadRepository, PettyCashFundReviewerReadRepository>();

        // Petty-cash module, chunk 2-ب — file attachments side table (same doc, پیوست section).
        services.AddScoped<IPettyCashAttachmentRepository, PettyCashAttachmentRepository>();
        services.AddScoped<IPettyCashAttachmentReadRepository, PettyCashAttachmentReadRepository>();

        // Petty-cash module, بخش ۳-الف (۲۰۲۶-۰۹-۲۸) — ترمیم/شارژ و استرداد وجه (same doc، «بخش ۳ —
        // طراحی»).
        services.AddScoped<IPettyCashReplenishmentRepository, PettyCashReplenishmentRepository>();
        services.AddScoped<IPettyCashReplenishmentReadRepository, PettyCashReplenishmentReadRepository>();
        services.AddScoped<IPettyCashRefundRepository, PettyCashRefundRepository>();
        services.AddScoped<IPettyCashRefundReadRepository, PettyCashRefundReadRepository>();
        services.AddScoped<IPettyCashLedgerReadRepository, PettyCashLedgerReadRepository>();

        // Petty-cash module, بخش ۳-ب (۲۰۲۶-۰۹-۲۸) — تسویهٔ دوره و سند GL (same doc §۹).
        services.AddScoped<IPettyCashSettlementPeriodRepository, PettyCashSettlementPeriodRepository>();
        services.AddScoped<IPettyCashSettlementReadRepository, PettyCashSettlementReadRepository>();

        // خزانه‌داری، بخش ۴-الف (۲۰۲۶-۰۹-۲۸) — درخواست پرداخت + کارتابل تأیید. جدول‌های جانبی
        // کاملاً جدید، همان استثنای صریح صاحب پروژه (docs/tankhah-khazaneh-module.md §۱۰).
        services.AddScoped<ITreasurySettingRepository, TreasurySettingRepository>();
        services.AddScoped<ITreasurySettingReadRepository, TreasurySettingReadRepository>();

        // صورت‌های مالی، بخش ۴۵-الف — قالب/نسخه/ردیف (docs/fs-module.md).
        services.AddScoped<IFsTemplateRepository, FsTemplateRepository>();
        services.AddScoped<IFsTemplateReadRepository, FsTemplateReadRepository>();

        // صورت‌های مالی، بخش ۴۵-ب — مانده از اسناد + اجرا/Snapshot.
        services.AddScoped<IFsBalanceReadRepository, FsBalanceReadRepository>();
        services.AddScoped<IFsRunRepository, FsRunRepository>();
        services.AddScoped<IFsCheckRuleRepository, FsCheckRuleRepository>();
        services.AddScoped<IFsApprovalStepRepository, FsApprovalStepRepository>();
        services.AddScoped<IFsPeriodRepository, FsPeriodRepository>();
        services.AddScoped<IFsNarrativeRepository, FsNarrativeRepository>();
        services.AddScoped<IFsRatioRepository, FsRatioRepository>();
        services.AddScoped<IFsPermissionRepository, FsPermissionRepository>();
        services.AddScoped<IFsConsolidationRepository, FsConsolidationRepository>();

        // صورت‌های مالی، بخش ۴۵-د — خروجی Excel (ClosedXML)، بی‌حالت.
        services.AddSingleton<IFsExcelExporter, Accounting.Infrastructure.Reporting.FsExcelExporter>();
        services.AddSingleton<IFsDocxExporter, Accounting.Infrastructure.Reporting.FsDocxExporter>();
        services.AddScoped<ITreasuryRoleRepository, TreasuryRoleRepository>();
        services.AddScoped<ITreasuryRoleReadRepository, TreasuryRoleReadRepository>();
        services.AddScoped<IPaymentRequestRepository, PaymentRequestRepository>();
        services.AddScoped<IPaymentRequestReadRepository, PaymentRequestReadRepository>();
        services.AddScoped<IPaymentRequestEventRepository, PaymentRequestEventRepository>();
        services.AddScoped<IPaymentRequestEventReadRepository, PaymentRequestEventReadRepository>();
        // اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — پیکر «تفصیلی ذی‌نفع».
        services.AddScoped<ITreasuryBeneficiaryTafsiliReadRepository, TreasuryBeneficiaryTafsiliReadRepository>();

        // بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — extracted شرکت‌شده در بخش ۴-ج هم: پروجکشن سند GL برای
        // GET .../accounting endpointها (docs/tankhah-khazaneh-module.md §۱۰).
        services.AddScoped<IVoucherAccountingReader, VoucherAccountingReader>();

        // خزانه‌داری، بخش ۴-ج (۲۰۲۶-۰۹-۲۹) — دریافت وجه + انتقال وجه.
        services.AddScoped<ITreasuryReceiptRepository, TreasuryReceiptRepository>();
        services.AddScoped<ITreasuryReceiptReadRepository, TreasuryReceiptReadRepository>();
        services.AddScoped<ITreasuryTransferRepository, TreasuryTransferRepository>();
        services.AddScoped<ITreasuryTransferReadRepository, TreasuryTransferReadRepository>();
        services.AddScoped<ITreasuryTransferEventRepository, TreasuryTransferEventRepository>();
        services.AddScoped<ITreasuryBankAccountBalanceReadRepository, TreasuryBankAccountBalanceReadRepository>();

        // خزانه‌داری، بخش ۴-د (۲۰۲۶-۰۹-۲۹) — مغایرت‌گیری بانکی + داشبورد خزانه.
        services.AddScoped<ITreasuryBankStatementRepository, TreasuryBankStatementRepository>();
        services.AddScoped<ITreasuryBankStatementReadRepository, TreasuryBankStatementReadRepository>();
        services.AddScoped<ITreasuryBankStatementLineRepository, TreasuryBankStatementLineRepository>();
        services.AddScoped<IBankStatementBookCandidateReadRepository, BankStatementBookCandidateReadRepository>();
        services.AddScoped<ITreasuryDashboardReadRepository, TreasuryDashboardReadRepository>();
        // دیسکت حساب جاری بانک رفاه (STM001، ۱۳۹ کاراکتری) — قالب عین ImportDisketCommandHandler مرجع
        // (۲۰۲۶-۱۰-۰۵). بانک دیگری که آمد، پیاده‌سازی جدا و انتخاب بر اساس بانکِ حساب.
        services.AddScoped<IBankStatementFileParser, Accounting.Infrastructure.Services.RefahBankStatementFileParser>();

        services.AddTaminTokenManager(config => PopulateTokenManagerConfiguration(config, configuration));

        // نقش‌های کاربر از پورتال سامانهٔ ورود (عین CurrentUserRepository سیستم قدیم).
        services.AddMemoryCache();
        services.AddHttpClient(nameof(Accounting.Infrastructure.Idp.IdpPortalUserRoleProvider), c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<IUserRoleProvider, Accounting.Infrastructure.Idp.IdpPortalUserRoleProvider>();

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
