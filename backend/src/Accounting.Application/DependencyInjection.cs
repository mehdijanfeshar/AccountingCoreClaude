using System.Reflection;
using Accounting.Application.Accounts.Commands.Common;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Vouchers.Commands.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR (Commands/Queries/Handlers), all FluentValidation validators found
    /// in this assembly, and the pipeline behaviors that run before every request reaches its
    /// handler: <see cref="VahedScopeBehavior{TRequest,TResponse}"/> then
    /// <see cref="ValidationBehavior{TRequest,TResponse}"/>.
    ///
    /// <b>Registration order matters.</b> MediatR runs registered <c>IPipelineBehavior</c>
    /// instances outermost-first, in registration order, so <c>VahedScopeBehavior</c> — being
    /// registered first — wraps <c>ValidationBehavior</c>, not the other way round. This is
    /// deliberate: authorization (who is allowed to write into which unit) must be resolved
    /// before syntactic validation runs, and concretely, any <c>RuleFor(x => x.VahedCode)</c> in
    /// a FluentValidation validator must see the server-assigned value, not whatever the caller
    /// sent — if <c>ValidationBehavior</c> ran first, it would validate a value that
    /// <c>VahedScopeBehavior</c> is about to discard and replace.
    ///
    /// ✅ <b>Fixed in phase 31 — both behaviors now declare only <c>where TRequest : notnull</c>.</b>
    /// <c>ValidationBehavior</c> used to declare <c>where TRequest : IRequest&lt;TResponse&gt;</c>,
    /// which (verified empirically against this project's actual MediatR 14.2.0) is not satisfied
    /// by a void command — <c>: IRequest</c> with no generic response, i.e. every
    /// <c>Update</c>/<c>Delete</c> command here — because <c>IRequest</c> does not implement
    /// <c>IRequest&lt;Unit&gt;</c> in this MediatR version. The .NET DI container's open-generic
    /// resolution skipped the registration silently, so from phase 8 to phase 30 no Update or
    /// Delete command was ever validated by the pipeline. <b>Do not reintroduce that constraint
    /// on either behavior</b> — on <c>ValidationBehavior</c> it disables validation, on
    /// <c>VahedScopeBehavior</c> it reopens IDOR risk #1; <c>BehaviorPipelineConstraintTests</c>
    /// fails if either one grows a constraint again.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(applicationAssembly));

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(VahedScopeBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Not a pipeline behavior and not a repository: a piece of write-side domain logic shared
        // by the three «ارتباط معین با گروه تفصیلی» handlers, which keeps TB_ACCOUNT_LINK_LEVEL in
        // step with TB_ACCOUNT_LINK_TAFSILGROUP. Scoped, so it joins the caller's unit of work.
        services.AddScoped<AccountLevelLinkSynchronizer>();

        // صورت‌های مالی (فاز ۴۵) — قواعد تفکیک واحد (دیدن/تغییر قالب، اولویت قالب در اجرا، گروه سهم واحدها).
        services.AddScoped<Accounting.Application.FinancialStatements.IFsUnitScopeProvider,
            Accounting.Application.FinancialStatements.FsUnitScopeProvider>();
        services.AddScoped<Accounting.Application.FinancialStatements.Queries.Drill.FsDrillVoucherReader>();
        services.AddScoped<Accounting.Application.FinancialStatements.Periods.FsPeriodGuard>();

        // Scoped, not transient, on purpose: it memoises the per-معین level lookup for the
        // lifetime of one request, which is what keeps a composite create with many lines from
        // issuing one database read per line.
        services.AddScoped<IVoucherTafsiliLevelGuard, VoucherTafsiliLevelGuard>();

        // Petty-cash module, chunk 1 — the three §4 rules that apply only at Submit, shared by
        // CreatePettyCashExpenseDoc (when submit=true) and SubmitPettyCashExpenseDoc so the two
        // paths cannot drift apart. See IPettyCashSubmitRuleChecker XML doc.
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashSubmitRuleChecker,
            Accounting.Application.PettyCash.Commands.Common.PettyCashSubmitRuleChecker>();

        // Petty-cash module, chunk 2 (بخش ۲) — the SoD authorizer and the shared review-transition
        // service used by StartReview/Return/Reject/BulkApprove. See
        // IPettyCashReviewAuthorizer / IPettyCashReviewTransitionService XML docs.
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashReviewAuthorizer,
            Accounting.Application.PettyCash.Commands.Common.PettyCashReviewAuthorizer>();
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashReviewTransitionService,
            Accounting.Application.PettyCash.Commands.Common.PettyCashReviewTransitionService>();

        // تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، تأیید دومرحله‌ای) — final approval, shared by
        // ApprovePettyCashExpenseDoc and BulkApprovePettyCashExpenseDocs. See
        // IPettyCashFinalApprovalService XML doc.
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashFinalApprovalService,
            Accounting.Application.PettyCash.Commands.Common.PettyCashFinalApprovalService>();

        // بخش ۳-الف (۲۰۲۶-۰۹-۲۸) — role authorizer shared by every ترمیم command. See
        // IPettyCashReplenishmentAuthorizer XML doc.
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashReplenishmentAuthorizer,
            Accounting.Application.PettyCash.Commands.Common.PettyCashReplenishmentAuthorizer>();
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashRefundRecorderAuthorizer,
            Accounting.Application.PettyCash.Commands.Common.PettyCashRefundRecorderAuthorizer>();

        // بخش ۳-ب (۲۰۲۶-۰۹-۲۸) — settlement period boundaries/opening balance (shared by the
        // preview query and the count/finalize commands) and the settlement GL voucher builder
        // (shared shape with every other voucher write path — see its own XML doc).
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashSettlementPeriodProvisioner,
            Accounting.Application.PettyCash.Commands.Common.PettyCashSettlementPeriodProvisioner>();
        services.AddScoped<Accounting.Application.PettyCash.Commands.Common.IPettyCashSettlementVoucherBuilder,
            Accounting.Application.PettyCash.Commands.Common.PettyCashSettlementVoucherBuilder>();

        // خزانه‌داری، بخش ۴-الف (۲۰۲۶-۰۹-۲۸) — درخواست پرداخت + کارتابل تأیید
        // (docs/tankhah-khazaneh-module.md §۱۰).
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.ITreasuryRoleAuthorizer,
            Accounting.Application.Treasury.Commands.Common.TreasuryRoleAuthorizer>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestSubmitRuleChecker,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestSubmitRuleChecker>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestApprovalService,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestApprovalService>();

        // اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹) — تفصیلی چندسطحی مرکز هزینه + تفصیلی ذی‌نفع اختیاری، مشترک
        // بین Create/UpdatePaymentRequest. See IPaymentRequestTafsiliValidator XML doc.
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestTafsiliValidator,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestTafsiliValidator>();

        // خزانه‌داری، بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — اجرای پرداخت + دو سند GL خودکار
        // (docs/tankhah-khazaneh-module.md §۱۰). Scoped: PaymentRequestLiabilityVoucherBuilder
        // memoises its per-(vahedCode,year) DOC_NUM reservation for the lifetime of one request —
        // see its own XML doc for why bulk-approve needs that.
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestPayablesTafsiliResolver,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestPayablesTafsiliResolver>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestLiabilityVoucherBuilder,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestLiabilityVoucherBuilder>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestPaymentVoucherBuilder,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestPaymentVoucherBuilder>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IPaymentRequestExecutionAuthorizer,
            Accounting.Application.Treasury.Commands.Common.PaymentRequestExecutionAuthorizer>();

        // خزانه‌داری، بخش ۴-ج (۲۰۲۶-۰۹-۲۹) — دریافت وجه + انتقال وجه
        // (docs/tankhah-khazaneh-module.md §۱۰). Reuses بخش-۴-ب's PaymentRequestVoucherBuildResult
        // shape and IPayReciveHeadRepository — see each type's own XML doc.
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.ITreasuryTreasurerAuthorizer,
            Accounting.Application.Treasury.Commands.Common.TreasuryTreasurerAuthorizer>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IReceiptPayerValidator,
            Accounting.Application.Treasury.Commands.Common.ReceiptPayerValidator>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IReceiptVoucherBuilder,
            Accounting.Application.Treasury.Commands.Common.ReceiptVoucherBuilder>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IReceiptRegistrationService,
            Accounting.Application.Treasury.Commands.Common.ReceiptRegistrationService>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.ITransferVoucherBuilder,
            Accounting.Application.Treasury.Commands.Common.TransferVoucherBuilder>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.ITransferApprovalService,
            Accounting.Application.Treasury.Commands.Common.TransferApprovalService>();

        // خزانه‌داری، بخش ۴-د — مغایرت‌گیری بانکی. IBankStatementFileParser عمداً ثبت نشده (قالب
        // دیسکت بانک هنوز نیامده)؛ ImportBankStatementCommandHandler آن را اختیاری resolve می‌کند.
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IBankFeeVoucherBuilder,
            Accounting.Application.Treasury.Commands.Common.BankFeeVoucherBuilder>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IBankStatementAutoMatchService,
            Accounting.Application.Treasury.Commands.Common.BankStatementAutoMatchService>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IBankStatementManualMatchService,
            Accounting.Application.Treasury.Commands.Common.BankStatementManualMatchService>();
        services.AddScoped<Accounting.Application.Treasury.Commands.Common.IBankStatementLineResolutionService,
            Accounting.Application.Treasury.Commands.Common.BankStatementLineResolutionService>();

        return services;
    }
}
