using Accounting.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api;

/// <summary>
/// Centralized exception-to-HTTP-response translation for every controller in the API.
/// Registered via <c>AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in <c>Program.cs</c>; no controller catches exceptions
/// itself. Mapping:
/// <list type="bullet">
/// <item><description><see cref="ValidationException"/> (FluentValidation, raised by
/// <c>ValidationBehavior</c>) → 400 <see cref="HttpValidationProblemDetails"/> with a
/// field→messages dictionary built from <see cref="ValidationException.Errors"/>.</description></item>
/// <item><description><see cref="DuplicateKeyException"/> (raised by
/// <c>UnitOfWork.SaveChangesAsync</c> after translating an Oracle ORA-00001) → 409
/// <see cref="ProblemDetails"/> with a safe, generic message — never the raw Oracle/SQL
/// text.</description></item>
/// <item><description><see cref="ForeignKeyViolationException"/> (raised by
/// <c>UnitOfWork.SaveChangesAsync</c> after translating an Oracle ORA-02291) → 400
/// <see cref="ProblemDetails"/> with a safe, generic message — never the raw Oracle/SQL text,
/// which would name the violated constraint, table and column. 400 rather than 409 because a
/// missing parent key means the caller sent an identifier that references nothing (an invalid
/// payload), not a conflict with existing state — see <see cref="ForeignKeyViolationException"/>
/// XML doc for the full rationale, including why 404 was rejected. Note this is the one 400
/// response on the API that carries a plain <see cref="ProblemDetails"/> rather than an
/// <see cref="HttpValidationProblemDetails"/> with an <c>errors</c> dictionary, since there is no
/// request field to attribute the failure to without leaking the Oracle constraint
/// name.</description></item>
/// <item><description><see cref="NotFoundException"/> (raised by Update/Delete command
/// handlers when the target row does not exist or is already soft-deleted) → 404
/// <see cref="ProblemDetails"/> with a safe, generic message — no table/column name
/// leaked.</description></item>
/// <item><description><see cref="MissingVahedScopeException"/> (raised by
/// <c>VahedScopeBehavior</c> when the authenticated caller has no usable
/// <c>VahedCode</c>/unit-scope claim) → 403 <see cref="ProblemDetails"/>. Deliberately 403, not
/// 401: the caller is authenticated (401 already applies to unauthenticated requests via the
/// fallback authorization policy), it simply lacks a claim usable for this
/// operation.</description></item>
/// <item><description>Anything else → 500 generic <see cref="ProblemDetails"/> with no stack
/// trace in the body. The original exception is still logged via <see cref="ILogger"/>.</description></item>
/// </list>
/// Writes the response through <see cref="IProblemDetailsService"/> (registered by
/// <c>AddProblemDetails()</c> in <c>Program.cs</c>) rather than hand-serializing JSON, so the
/// response gets the correct <c>application/problem+json</c> content type and honours any
/// <c>CustomizeProblemDetails</c> hook registered elsewhere.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                (ProblemDetails)BuildValidationProblemDetails(validationException, httpContext)),

            DuplicateKeyException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    "A row with the same unique key already exists.")),

            ForeignKeyViolationException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    "One or more referenced records do not exist.")),

            // The only mapping here that returns a per-exception detail rather than a fixed
            // string. That is deliberate and safe: what it carries is a تفصیلی level's business
            // name, which the caller already sees on the form — not a Legacy table, column or
            // constraint name. See TafsiliLevelRuleException for the reasoning.
            TafsiliLevelRuleException tafsiliLevelRuleException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    tafsiliLevelRuleException.PublicDetail)),

            // State-based refusal, not a permissions one: the caller MAY edit this voucher, just
            // not while it is reviewed/accepted — and they can fix that themselves via
            // change-state. See VoucherNotEditableException for why 409 rather than 403.
            // Terminal-state refusal: تأیید دائم can never be moved. Separate from
            // VoucherNotEditableException because that one offers a way out and this one cannot.
            VoucherStateChangeDeniedException voucherStateChangeDeniedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    voucherStateChangeDeniedException.PublicDetail)),

            VoucherNotEditableException voucherNotEditableException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    voucherNotEditableException.PublicDetail)),

            // Petty-cash module, chunk 1. Same state-based-refusal shape as
            // VoucherNotEditableException above, just for TB_PC_EXPENSE_DOC's own state machine.
            PettyCashDocNotEditableException pettyCashDocNotEditableException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashDocNotEditableException.PublicDetail)),

            // Application-level duplicate (no DB UNIQUE constraint backs it — a rejected or
            // soft-deleted document must not block reuse of the same invoice number). 409: the
            // request itself is well-formed, it conflicts with another existing document.
            PettyCashDuplicateExpenseDocException pettyCashDuplicateExpenseDocException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashDuplicateExpenseDocException.PublicDetail)),

            // The three Submit-only §4 rules — all 400, since each is "this specific amount,
            // right now, does not fit", not a conflict with another resource.
            PettyCashPerDocLimitExceededException pettyCashPerDocLimitExceededException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    pettyCashPerDocLimitExceededException.PublicDetail)),

            PettyCashInsufficientCashBalanceException pettyCashInsufficientCashBalanceException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    pettyCashInsufficientCashBalanceException.PublicDetail)),

            PettyCashInvoiceYearMismatchException pettyCashInvoiceYearMismatchException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    pettyCashInvoiceYearMismatchException.PublicDetail)),

            // Petty-cash module, chunk 2-ب — attachment content exceeds the 10 MiB cap. 400: the
            // request's own payload does not fit a rule, same shape as the three §4 rules above.
            PettyCashAttachmentTooLargeException pettyCashAttachmentTooLargeException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    pettyCashAttachmentTooLargeException.PublicDetail)),

            // Petty-cash module, chunk 2 (بخش ۲). Wrong DOC_STATE for the requested review
            // action — same state-based-refusal shape as PettyCashDocNotEditableException.
            PettyCashReviewStateConflictException pettyCashReviewStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashReviewStateConflictException.PublicDetail)),

            // SoD part (ب): caller IS an active reviewer for the fund, but also created this very
            // document — a conflict-of-interest state, not a permissions gap, hence 409.
            PettyCashSelfReviewConflictException pettyCashSelfReviewConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSelfReviewConflictException.PublicDetail)),

            // BulkApprove all-or-nothing failure — one 409 regardless of how many/which ids
            // failed or why; the per-id reasons ride along as a ProblemDetails extension so the
            // caller can react per row without a second round-trip.
            PettyCashBulkApproveConflictException pettyCashBulkApproveConflictException => (
                StatusCodes.Status409Conflict,
                BuildBulkApproveConflictProblemDetails(httpContext, pettyCashBulkApproveConflictException)),

            // Petty-cash module, 2026-09-28 decision (TB_PC_FUND). Application-level duplicate
            // code check — same reasoning as PettyCashDuplicateExpenseDocException above.
            PettyCashFundCodeDuplicateException pettyCashFundCodeDuplicateException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashFundCodeDuplicateException.PublicDetail)),

            // Inactive-fund refusal on Create/Submit — state-based, same shape as
            // PettyCashDocNotEditableException.
            PettyCashFundInactiveException pettyCashFundInactiveException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashFundInactiveException.PublicDetail)),

            // Delete guard: fund still has live صورت‌هزینه rows.
            PettyCashFundHasExpenseDocsException pettyCashFundHasExpenseDocsException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashFundHasExpenseDocsException.PublicDetail)),

            NotFoundException => (
                StatusCodes.Status404NotFound,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    "The requested resource was not found.")),

            MissingVahedScopeException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "The authenticated caller has no usable organizational-unit scope for this operation.")),

            // Scope-level 403: the request asked to BE a unit the caller may not be, decided
            // before any row is touched — as opposed to the record-level
            // UnitAccessDeniedException below. See UnitActAsDeniedException for why they are not
            // collapsed. Carries a per-exception detail for the same reason
            // TafsiliLevelRuleException does: the value echoed back is the unit code the caller
            // themselves just sent.
            UnitActAsDeniedException unitActAsDeniedException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    unitActAsDeniedException.PublicDetail)),

            // A fourth distinct 403: the token names a unit, but no TB_VAHED_INFO row carries that
            // code — an IDP/Legacy data mismatch rather than a permissions decision. The unit code
            // echoed back is the caller's own, which their token already carries.
            UnknownCallerUnitException unknownCallerUnitException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    unknownCallerUnitException.PublicDetail)),

            // Deliberately a different detail string from MissingVahedScopeException above, even
            // though both are 403: that one means "we could not work out which unit you are", this
            // one means "we know which unit you are, and this record is not yours". Collapsing them
            // would make a misconfigured token and a cross-unit access attempt indistinguishable in
            // support. Neither body names the owning unit — that stays in the log.
            UnitAccessDeniedException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    "This record belongs to a different organizational unit.")),

            // Petty-cash module, chunk 2 (بخش ۲), SoD part (الف): caller has no active
            // TB_PC_REVIEWER row for this document's fund at all — a straight permissions gap,
            // same shape as UnitAccessDeniedException above.
            PettyCashReviewerAccessDeniedException pettyCashReviewerAccessDeniedException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashReviewerAccessDeniedException.PublicDetail)),

            // Petty-cash module, chunk 2-ب: attachment add/delete is owner-only, not a
            // بررسی‌کننده action — plain permissions gap, same shape as the reviewer one above.
            PettyCashAttachmentOwnerOnlyException pettyCashAttachmentOwnerOnlyException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashAttachmentOwnerOnlyException.PublicDetail)),

            // Petty-cash module, تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸): create/submit/update/delete is
            // custodian-only — plain permissions gap, same shape as the two above.
            PettyCashNotCustodianException pettyCashNotCustodianException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashNotCustodianException.PublicDetail)),

            // Two-stage approval (تکمیل بخش ۲): final approval attempted before an inspector
            // verified control, or verify attempted twice — both state conflicts, same shape as
            // PettyCashReviewStateConflictException.
            PettyCashNotVerifiedException pettyCashNotVerifiedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashNotVerifiedException.PublicDetail)),

            PettyCashAlreadyVerifiedException pettyCashAlreadyVerifiedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashAlreadyVerifiedException.PublicDetail)),

            // Caller holds a final-approval-capable role but not enough authority for this
            // amount — distinct from PettyCashReviewerAccessDeniedException (no role at all).
            PettyCashApprovalAuthorityExceededException pettyCashApprovalAuthorityExceededException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashApprovalAuthorityExceededException.PublicDetail)),

            // Second SoD rule (تکمیل بخش ۲): verifier cannot also give final approval — same
            // conflict-of-interest shape as PettyCashSelfReviewConflictException.
            PettyCashVerifierCannotApproveException pettyCashVerifierCannotApproveException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashVerifierCannotApproveException.PublicDetail)),

            // Field-by-field lock on a Returned document (تکمیل بخش ۲، صفحهٔ ۸) — caller may edit
            // the document right now, just not these particular fields.
            PettyCashReturnFieldLockedException pettyCashReturnFieldLockedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashReturnFieldLockedException.PublicDetail)),

            // بخش ۳-الف (۲۰۲۶-۰۹-۲۸) — ترمیم/شارژ و استرداد وجه. No documents to build a ترمیم
            // from — 409, same shape as PettyCashFundHasExpenseDocsException.
            PettyCashNoDocumentsToReplenishException pettyCashNoDocumentsToReplenishException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashNoDocumentsToReplenishException.PublicDetail)),

            // Race guard: a صورت‌هزینه already picked up by another concurrent ترمیم request.
            PettyCashDocAlreadyReplenishedException pettyCashDocAlreadyReplenishedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashDocAlreadyReplenishedException.PublicDetail)),

            // Wrong STATE for a ترمیم action — same state-based-refusal shape as
            // PettyCashReviewStateConflictException.
            PettyCashReplenishmentStateConflictException pettyCashReplenishmentStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashReplenishmentStateConflictException.PublicDetail)),

            // Caller holds none of the roles a ترمیم action requires — straight permissions gap.
            PettyCashReplenishmentRoleRequiredException pettyCashReplenishmentRoleRequiredException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashReplenishmentRoleRequiredException.PublicDetail)),

            // SoD: ترمیم's own creator cannot also approve it.
            PettyCashReplenishmentApproverConflictException pettyCashReplenishmentApproverConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashReplenishmentApproverConflictException.PublicDetail)),

            // SoD: ترمیم's own approver cannot also record its payment.
            PettyCashReplenishmentPayerConflictException pettyCashReplenishmentPayerConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashReplenishmentPayerConflictException.PublicDetail)),

            // Defensive ceiling guard on ترمیم totals — 400, same shape as the §4 Submit rules.
            PettyCashReplenishmentExceedsCeilingException pettyCashReplenishmentExceedsCeilingException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    pettyCashReplenishmentExceedsCeilingException.PublicDetail)),

            // Caller is not TB_PC_FUND.REFUND_RECORDER's designated recorder for this fund.
            PettyCashRefundRecorderMismatchException pettyCashRefundRecorderMismatchException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashRefundRecorderMismatchException.PublicDetail)),

            // بخش ۳-ب (تسویهٔ دوره) — straight permissions gap: caller is not an active
            // SeniorAccountant reviewer of the fund.
            PettyCashSettlementRoleRequiredException pettyCashSettlementRoleRequiredException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    pettyCashSettlementRoleRequiredException.PublicDetail)),

            // SoD — caller created at least one of the صورت‌هزینه rows being settled.
            PettyCashSettlementSoDConflictException pettyCashSettlementSoDConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementSoDConflictException.PublicDetail)),

            // Missing acknowledgeInFlightTransfer=true on the request itself — 400, fixable by
            // resubmitting with the flag set.
            PettyCashSettlementInFlightAcknowledgeRequiredException pettyCashSettlementInFlightAcknowledgeRequiredException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    pettyCashSettlementInFlightAcknowledgeRequiredException.PublicDetail)),

            // Draft period has no COUNTED_BALANCE yet — state conflict, same shape as
            // PettyCashReplenishmentStateConflictException.
            PettyCashSettlementCountedBalanceRequiredException pettyCashSettlementCountedBalanceRequiredException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementCountedBalanceRequiredException.PublicDetail)),

            // Recorded COUNTED_BALANCE disagrees with the computed closing cash balance.
            PettyCashSettlementCountedBalanceMismatchException pettyCashSettlementCountedBalanceMismatchException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementCountedBalanceMismatchException.PublicDetail)),

            // No صورت‌هزینه eligible for this settlement — same "nothing to act on" shape as
            // PettyCashNoDocumentsToReplenishException.
            PettyCashSettlementNoDocumentsException pettyCashSettlementNoDocumentsException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementNoDocumentsException.PublicDetail)),

            // Fund has no حساب معین configured — settlement voucher credit line cannot be built.
            PettyCashSettlementFundAccountMissingException pettyCashSettlementFundAccountMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementFundAccountMissingException.PublicDetail)),

            // A مادهٔ هزینه in the settlement has no حساب معین configured.
            PettyCashSettlementExpenseAccountMissingException pettyCashSettlementExpenseAccountMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementExpenseAccountMissingException.PublicDetail)),

            // تفصیلی الزامی violation on a server-derived settlement voucher line — 409, not 400
            // (see that exception's own XML doc for why).
            PettyCashSettlementTafsiliMissingException pettyCashSettlementTafsiliMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementTafsiliMissingException.PublicDetail)),

            // Defensive unbalanced-voucher guard — should never actually fire; see its XML doc.
            PettyCashSettlementUnbalancedException pettyCashSettlementUnbalancedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashSettlementUnbalancedException.PublicDetail)),

            // A استرداد's own date falls inside an already-finalized settlement period.
            PettyCashRefundLockedBySettledPeriodException pettyCashRefundLockedBySettledPeriodException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    pettyCashRefundLockedBySettledPeriodException.PublicDetail)),

            // خزانه‌داری، بخش ۴-الف (۲۰۲۶-۰۹-۲۸) — درخواست پرداخت. Wrong REQUEST_STATE for the
            // requested action — same state-based-refusal shape as PettyCashReviewStateConflictException.
            // صورت‌های مالی (۴۵) — قالب واحد دیگر یا قالب مشترک از غیرستاد.
            FsAccessDeniedException fsAccessDeniedException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    fsAccessDeniedException.PublicDetail)),

            // صورت‌های مالی (۴۵-الف) — ویرایش نسخهٔ غیرپیش‌نویس، دو پیش‌نویس، کد تکراری، حذف قالب فعال.
            FsTemplateConflictException fsTemplateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    fsTemplateConflictException.PublicDetail)),

            // اعلامیه — گذار وضعیت نامجاز، ویرایش اعلامیهٔ دارای سند، حساب رابط تعریف‌نشده.
            // کارت حساب جاری — ردیف مغایرت‌گیری‌شده، ماه دیسکت و …
            BankCardConflictException bankCardConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    bankCardConflictException.PublicDetail)),

            ElamConflictException elamConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    elamConflictException.PublicDetail)),

            PaymentRequestStateConflictException paymentRequestStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestStateConflictException.PublicDetail)),

            // Update/Delete/Submit is creator-only — plain permissions gap, same shape as
            // PettyCashNotCustodianException.
            PaymentRequestNotCreatorException paymentRequestNotCreatorException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    paymentRequestNotCreatorException.PublicDetail)),

            // Caller holds none of the roles the request's current approval stage requires.
            PaymentRequestRoleRequiredException paymentRequestRoleRequiredException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    paymentRequestRoleRequiredException.PublicDetail)),

            // Unit-wide خزانه‌داری admin gate (settings/roles CRUD) — فقط FinanceManager واحد، با
            // استثنای bootstrap.
            TreasuryRoleRequiredException treasuryRoleRequiredException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    treasuryRoleRequiredException.PublicDetail)),

            // SoD: تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کننده — conflict-of-interest, not a
            // permissions gap.
            PaymentRequestApproverConflictException paymentRequestApproverConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestApproverConflictException.PublicDetail)),

            // SoD: یک کاربر نمی‌تواند دو مرحلهٔ متوالی یک درخواست را تأیید کند.
            PaymentRequestConsecutiveApproverConflictException paymentRequestConsecutiveApproverConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestConsecutiveApproverConflictException.PublicDetail)),

            // Submit refused: unit has no TB_TR_SETTING row yet.
            PaymentRequestSettingsMissingException paymentRequestSettingsMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestSettingsMissingException.PublicDetail)),

            // اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹): BeneficiaryTafsiliId سِت شده اما واحد گروه تفصیلی ذی‌نفع
            // ندارد — همان شکل PaymentRequestSettingsMissingException (409، مشکل تنظیمات واحد).
            PaymentRequestBeneficiaryGroupNotConfiguredException paymentRequestBeneficiaryGroupNotConfiguredException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestBeneficiaryGroupNotConfiguredException.PublicDetail)),

            // اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹): گروه تعریف شده، ولی تفصیلی ارسالی عضوش نیست — خطای مقدار
            // ورودی، نه تعارض تنظیمات (400).
            PaymentRequestBeneficiaryTafsiliNotInGroupException paymentRequestBeneficiaryTafsiliNotInGroupException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    paymentRequestBeneficiaryTafsiliNotInGroupException.PublicDetail)),

            // Submit-only rules — 400, same shape as the petty-cash §4 Submit rules.
            PaymentRequestDueDatePastException paymentRequestDueDatePastException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    paymentRequestDueDatePastException.PublicDetail)),

            PaymentRequestInvoiceNotApprovedException paymentRequestInvoiceNotApprovedException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    paymentRequestInvoiceNotApprovedException.PublicDetail)),

            // Application-level duplicate (no DB UNIQUE constraint backs it) — 409.
            PaymentRequestDuplicateException paymentRequestDuplicateException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestDuplicateException.PublicDetail)),

            // Only ever thrown inside bulk-approve mode (folded into failedIds below); mapped
            // defensively in case it ever surfaces on its own.
            PaymentRequestBulkLimitExceededException paymentRequestBulkLimitExceededException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestBulkLimitExceededException.PublicDetail)),

            // BulkApprove all-or-nothing failure — same failedIds-extension shape as
            // PettyCashBulkApproveConflictException.
            PaymentRequestBulkApproveConflictException paymentRequestBulkApproveConflictException => (
                StatusCodes.Status409Conflict,
                BuildPaymentRequestBulkApproveConflictProblemDetails(httpContext, paymentRequestBulkApproveConflictException)),

            // خزانه‌داری، بخش ۴-ب (۲۰۲۶-۰۹-۲۹) — اجرای پرداخت + دو سند GL خودکار.
            PaymentRequestTreasurySettingAccountMissingException paymentRequestTreasurySettingAccountMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestTreasurySettingAccountMissingException.PublicDetail)),

            PaymentRequestVoucherAccountConfigException paymentRequestVoucherAccountConfigException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestVoucherAccountConfigException.PublicDetail)),

            PaymentRequestPayablesBeneficiaryRequiredException paymentRequestPayablesBeneficiaryRequiredException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestPayablesBeneficiaryRequiredException.PublicDetail)),

            PaymentRequestVoucherTafsiliMissingException paymentRequestVoucherTafsiliMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestVoucherTafsiliMissingException.PublicDetail)),

            // Defensive — should never actually fire; see its XML doc.
            PaymentRequestVoucherUnbalancedException paymentRequestVoucherUnbalancedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestVoucherUnbalancedException.PublicDetail)),

            // SoD: اجراکننده ≠ ثبت‌کنندهٔ درخواست.
            PaymentRequestExecutorConflictException paymentRequestExecutorConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    paymentRequestExecutorConflictException.PublicDetail)),

            // فقط خزانه‌دار می‌تواند execute/suspend/resume کند — ۴۰۳، نه ۴۰۹.
            PaymentRequestTreasurerRoleRequiredException paymentRequestTreasurerRoleRequiredException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    paymentRequestTreasurerRoleRequiredException.PublicDetail)),

            // خزانه‌داری، بخش ۴-ج (۲۰۲۶-۰۹-۲۹) — دریافت وجه + انتقال وجه.
            TreasurySettingValueMissingException treasurySettingValueMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasurySettingValueMissingException.PublicDetail)),

            TreasuryVoucherAccountConfigException treasuryVoucherAccountConfigException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryVoucherAccountConfigException.PublicDetail)),

            // Defensive — same posture as PaymentRequestVoucherTafsiliMissingException.
            TreasuryVoucherTafsiliMissingException treasuryVoucherTafsiliMissingException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryVoucherTafsiliMissingException.PublicDetail)),

            // گروه تعریف شده، ولی تفصیلی ارسالی عضوش نیست — خطای مقدار ورودی (۴۰۰)، نه تعارض
            // تنظیمات (که TreasurySettingValueMissingException، ۴۰۹، پوشش می‌دهد).
            TreasuryTafsiliNotInGroupException treasuryTafsiliNotInGroupException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    treasuryTafsiliNotInGroupException.PublicDetail)),

            // فقط خزانه‌دار می‌تواند register/approve کند — ۴۰۳، نه ۴۰۹.
            TreasuryTreasurerRoleRequiredException treasuryTreasurerRoleRequiredException => (
                StatusCodes.Status403Forbidden,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    treasuryTreasurerRoleRequiredException.PublicDetail)),

            TreasuryReceiptStateConflictException treasuryReceiptStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryReceiptStateConflictException.PublicDetail)),

            // Application-level duplicate (no DB UNIQUE constraint backs it) — 409.
            TreasuryReceiptDuplicateBankReferenceException treasuryReceiptDuplicateBankReferenceException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryReceiptDuplicateBankReferenceException.PublicDetail)),

            TreasuryTransferStateConflictException treasuryTransferStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryTransferStateConflictException.PublicDetail)),

            // SoD: تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کنندهٔ انتقال.
            TreasuryTransferApproverConflictException treasuryTransferApproverConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryTransferApproverConflictException.PublicDetail)),

            // Blocking control (a) — موجودی حساب بانکی مبدأ کافی نیست.
            TreasuryTransferInsufficientBalanceException treasuryTransferInsufficientBalanceException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryTransferInsufficientBalanceException.PublicDetail)),

            // Blocking control (b) — سقف روزانهٔ انتقال.
            TreasuryTransferDailyLimitExceededException treasuryTransferDailyLimitExceededException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryTransferDailyLimitExceededException.PublicDetail)),

            // خزانه‌داری، بخش ۴-د (۲۰۲۶-۰۹-۲۹) — مغایرت‌گیری بانکی + داشبورد خزانه.
            TreasuryBankStatementStateConflictException treasuryBankStatementStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryBankStatementStateConflictException.PublicDetail)),

            TreasuryBankStatementLineStateConflictException treasuryBankStatementLineStateConflictException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryBankStatementLineStateConflictException.PublicDetail)),

            // Input mismatch (direction/amount) on manual match — ۴۰۰، نه تعارض وضعیت.
            TreasuryBankStatementMatchMismatchException treasuryBankStatementMatchMismatchException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    treasuryBankStatementMatchMismatchException.PublicDetail)),

            TreasuryBankStatementResolutionInvalidException treasuryBankStatementResolutionInvalidException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    treasuryBankStatementResolutionInvalidException.PublicDetail)),

            TreasuryBankStatementUnresolveNotAllowedException treasuryBankStatementUnresolveNotAllowedException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryBankStatementUnresolveNotAllowedException.PublicDetail)),

            // قالب فایل دیسکت بانک هنوز تعریف نشده — IBankStatementFileParser بدون پیاده‌سازی ثبت‌شده.
            // دیسکت بانک نامعتبر — پیام فارسی قابل نمایش.
            TreasuryBankStatementFileInvalidException treasuryBankStatementFileInvalidException => (
                StatusCodes.Status400BadRequest,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    treasuryBankStatementFileInvalidException.PublicDetail)),

            TreasuryBankStatementParserNotConfiguredException treasuryBankStatementParserNotConfiguredException => (
                StatusCodes.Status409Conflict,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    treasuryBankStatementParserNotConfiguredException.PublicDetail)),

            _ => (
                StatusCodes.Status500InternalServerError,
                BuildProblemDetails(
                    httpContext,
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred while processing the request.")),
        };

        // Correlation: lets an operator tie a client-reported error back to the matching log
        // line (both carry the same ASP.NET Core TraceIdentifier for this request).
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        LogException(exception, statusCode, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private void LogException(Exception exception, int statusCode, string traceId)
    {
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing request {TraceId}.", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Request {TraceId} failed with status code {StatusCode}.", traceId, statusCode);
        }
    }

    private static HttpValidationProblemDetails BuildValidationProblemDetails(
        ValidationException validationException,
        HttpContext httpContext)
    {
        var errors = validationException.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Instance = httpContext.Request.Path,
        };
    }

    private static ProblemDetails BuildProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
    }

    /// <summary>
    /// Same shape as <see cref="BuildProblemDetails"/> plus a <c>failedIds</c> extension —
    /// <c>[{ id, reason }, ...]</c> — so a bulk-approve caller can react per row without a second
    /// round-trip. <c>reason</c> is one of the fixed machine-readable codes documented on
    /// <see cref="PettyCashBulkApproveConflictException.Failures"/>.
    /// </summary>
    private static ProblemDetails BuildBulkApproveConflictProblemDetails(
        HttpContext httpContext,
        PettyCashBulkApproveConflictException exception)
    {
        var problemDetails = BuildProblemDetails(
            httpContext,
            StatusCodes.Status409Conflict,
            "Conflict",
            exception.PublicDetail);

        problemDetails.Extensions["failedIds"] = exception.Failures
            .Select(pair => new { id = pair.Key, reason = pair.Value })
            .ToArray();

        return problemDetails;
    }

    /// <summary>Same shape as <see cref="BuildBulkApproveConflictProblemDetails"/>, for the
    /// خزانه‌داری bulk-approve endpoint's own exception type.</summary>
    private static ProblemDetails BuildPaymentRequestBulkApproveConflictProblemDetails(
        HttpContext httpContext,
        PaymentRequestBulkApproveConflictException exception)
    {
        var problemDetails = BuildProblemDetails(
            httpContext,
            StatusCodes.Status409Conflict,
            "Conflict",
            exception.PublicDetail);

        problemDetails.Extensions["failedIds"] = exception.Failures
            .Select(pair => new { id = pair.Key, reason = pair.Value })
            .ToArray();

        return problemDetails;
    }
}
