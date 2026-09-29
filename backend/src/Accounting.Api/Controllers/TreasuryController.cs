using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.Common;
using Accounting.Application.Treasury.Commands.ApprovePaymentRequest;
using Accounting.Application.Treasury.Commands.BulkApprovePaymentRequests;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Application.Treasury.Commands.CreatePaymentRequest;
using Accounting.Application.Treasury.Commands.CreateTreasuryRole;
using Accounting.Application.Treasury.Commands.DeletePaymentRequest;
using Accounting.Application.Treasury.Commands.DeleteTreasuryRole;
using Accounting.Application.Treasury.Commands.ExecutePaymentRequest;
using Accounting.Application.Treasury.Commands.RejectPaymentRequest;
using Accounting.Application.Treasury.Commands.ResumePaymentRequest;
using Accounting.Application.Treasury.Commands.ReturnPaymentRequest;
using Accounting.Application.Treasury.Commands.SubmitPaymentRequest;
using Accounting.Application.Treasury.Commands.SuspendPaymentRequest;
using Accounting.Application.Treasury.Commands.UpdatePaymentRequest;
using Accounting.Application.Treasury.Commands.UpsertTreasurySetting;
using Accounting.Application.Treasury.Queries;
using Accounting.Application.Treasury.Queries.GetApprovalCartable;
using Accounting.Application.Treasury.Queries.GetBeneficiaryTafsilis;
using Accounting.Application.Treasury.Queries.GetPaymentRequestAccounting;
using Accounting.Application.Treasury.Queries.GetPaymentRequestById;
using Accounting.Application.Treasury.Queries.GetPaymentRequests;
using Accounting.Application.Treasury.Queries.GetTreasuryRoles;
using Accounting.Application.Treasury.Queries.GetTreasurySetting;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over خزانه‌داری، بخش ۴-الف — درخواست پرداخت + کارتابل تأیید
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Every action does nothing but: build a request →
/// send it through MediatR → map the result to an <see cref="IActionResult"/>. No PUT/DELETE
/// anywhere here — <c>POST {id}/update</c> and <c>POST {id}/delete</c>, same project-wide mandate
/// as every other controller.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b> via the API-wide fallback
/// authorization policy.
/// </summary>
[ApiController]
[Route("api/treasury")]
public sealed class TreasuryController : ControllerBase
{
    private readonly IMediator _mediator;

    public TreasuryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ==================== تنظیمات ====================

    /// <summary>تنظیمات خزانهٔ واحد، یا 404 اگر مدیر مالی هنوز تعریف نکرده است.</summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(TreasurySettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTreasurySettingQuery(), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// ایجاد یا جایگزینی تنظیمات خزانهٔ واحد. فقط نقش FinanceManager همان واحد. ۴۰۴ اگر
    /// <c>beneficiaryTafsilGroupId</c>/<c>payablesAccountId</c>/<c>vatCreditAccountId</c>/
    /// <c>insurancePayableAccountId</c> مقدار داشته باشند ولی وجود نداشته باشند.
    /// </summary>
    [HttpPost("settings")]
    [ProducesResponseType(typeof(TreasurySettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertSettings(
        [FromBody] UpsertTreasurySettingRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpsertTreasurySettingCommand(
                request.CeoApprovalThreshold,
                request.BulkApproveLimit,
                request.BeneficiaryTafsilGroupId,
                request.PayablesAccountId,
                request.VatCreditAccountId,
                request.InsurancePayableAccountId),
            cancellationToken);

        return Ok(result);
    }

    // ==================== نقش‌ها ====================

    /// <summary>هر نقش خزانهٔ فعال واحد.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<TreasuryRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTreasuryRolesQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// ایجاد/فعال‌سازی مجدد یک نقش خزانه. فقط نقش FinanceManager همان واحد — با استثنای bootstrap:
    /// اگر واحد هیچ FinanceManager فعالی ندارد، هر کاربر احراز‌شدهٔ واحد می‌تواند نقش ثبت کند.
    /// </summary>
    [HttpPost("roles")]
    [ProducesResponseType(typeof(CreateTreasuryRoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateTreasuryRoleRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateTreasuryRoleCommand(request.UserId, request.UserName, request.Role), cancellationToken);

        return Ok(new CreateTreasuryRoleResponse(id));
    }

    /// <summary>حذف نرم یک نقش خزانه. فقط نقش FinanceManager همان واحد.</summary>
    [HttpPost("roles/{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteTreasuryRoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTreasuryRoleCommand(id), cancellationToken);

        return Ok(new DeleteTreasuryRoleResponse(id));
    }

    // ==================== درخواست پرداخت ====================

    /// <summary>صفحه‌ای از درخواست‌های پرداخت، به‌همراه شمار هر وضعیت (بی‌اثر از فیلتر state).</summary>
    [HttpGet("payment-requests")]
    [ProducesResponseType(typeof(PaymentRequestListResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PaymentRequestState? state = null,
        [FromQuery] string? search = null,
        [FromQuery] bool forExecution = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetPaymentRequestsQuery(pageNumber, pageSize, state, search, forExecution), cancellationToken);

        return Ok(result);
    }

    /// <summary>یک درخواست پرداخت، با «گردش عملیات»، یا 404.</summary>
    [HttpGet("payment-requests/{id:guid}")]
    [ProducesResponseType(typeof(PaymentRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentRequestById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentRequestByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// ایجاد یک درخواست پرداخت به‌صورت پیش‌نویس، یا — وقتی <see cref="CreatePaymentRequestRequest.Submit"/>
    /// true باشد — ارسال بلافاصله به مرحلهٔ تأیید مدیر واحد.
    /// </summary>
    [HttpPost("payment-requests")]
    [ProducesResponseType(typeof(CreatePaymentRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePaymentRequest(
        [FromBody] CreatePaymentRequestRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePaymentRequestCommand(
            request.BeneficiaryName,
            request.BeneficiaryNationalId,
            request.BeneficiaryTafsiliId,
            request.PaymentType,
            request.InvoiceRef,
            request.InvoiceApproved,
            request.ExpenseAccountId,
            request.CostCenterTafsilis ?? Array.Empty<PaymentRequestTafsiliLinkInput>(),
            request.AmountBeforeTax,
            request.VatPercent,
            request.VatAmount,
            request.InsuranceDeductionPercent,
            request.InsuranceDeductionAmount,
            request.DueDate,
            request.PaymentAccountId,
            request.PaymentMethod,
            request.Description,
            request.Year,
            request.Submit);

        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetPaymentRequestById), new { id }, new CreatePaymentRequestResponse(id));
    }

    /// <summary>جایگزینی کامل یک درخواست پرداخت. فقط Draft/Returned و فقط ثبت‌کننده.</summary>
    [HttpPost("payment-requests/{id:guid}/update")]
    [ProducesResponseType(typeof(UpdatePaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePaymentRequest(
        Guid id, [FromBody] UpdatePaymentRequestRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePaymentRequestCommand(
            id,
            request.BeneficiaryName,
            request.BeneficiaryNationalId,
            request.BeneficiaryTafsiliId,
            request.PaymentType,
            request.InvoiceRef,
            request.InvoiceApproved,
            request.ExpenseAccountId,
            request.CostCenterTafsilis ?? Array.Empty<PaymentRequestTafsiliLinkInput>(),
            request.AmountBeforeTax,
            request.VatPercent,
            request.VatAmount,
            request.InsuranceDeductionPercent,
            request.InsuranceDeductionAmount,
            request.DueDate,
            request.PaymentAccountId,
            request.PaymentMethod,
            request.Description);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdatePaymentRequestResponse(id));
    }

    /// <summary>حذف نرم. فقط Draft/Returned و فقط ثبت‌کننده.</summary>
    [HttpPost("payment-requests/{id:guid}/delete")]
    [ProducesResponseType(typeof(DeletePaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePaymentRequest(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePaymentRequestCommand(id), cancellationToken);

        return Ok(new DeletePaymentRequestResponse(id));
    }

    /// <summary>Draft/Returned → PendingUnitManager. بدون بدنه. فقط ثبت‌کننده.</summary>
    [HttpPost("payment-requests/{id:guid}/submit")]
    [ProducesResponseType(typeof(SubmitPaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitPaymentRequest(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SubmitPaymentRequestCommand(id), cancellationToken);

        return Ok(new SubmitPaymentRequestResponse(id));
    }

    /// <summary>یک مرحله جلو می‌برد. فقط نقش مرحلهٔ فعلی، ≠ ثبت‌کننده، ≠ تأییدکنندهٔ مرحلهٔ قبل.</summary>
    [HttpPost("payment-requests/{id:guid}/approve")]
    [ProducesResponseType(typeof(ApprovePaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApprovePaymentRequest(
        Guid id, [FromBody] ApprovePaymentRequestRequest? request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ApprovePaymentRequestCommand(id, request?.Note), cancellationToken);

        return Ok(new ApprovePaymentRequestResponse(id));
    }

    /// <summary>به ثبت‌کننده برای اصلاح برمی‌گرداند. دلیل اجباری.</summary>
    [HttpPost("payment-requests/{id:guid}/return")]
    [ProducesResponseType(typeof(ReturnPaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReturnPaymentRequest(
        Guid id, [FromBody] ReturnPaymentRequestRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReturnPaymentRequestCommand(id, request.Reason), cancellationToken);

        return Ok(new ReturnPaymentRequestResponse(id));
    }

    /// <summary>رد پایانی. دلیل اجباری.</summary>
    [HttpPost("payment-requests/{id:guid}/reject")]
    [ProducesResponseType(typeof(RejectPaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectPaymentRequest(
        Guid id, [FromBody] RejectPaymentRequestRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RejectPaymentRequestCommand(id, request.Reason), cancellationToken);

        return Ok(new RejectPaymentRequestResponse(id));
    }

    /// <summary>
    /// تأیید گروهی — all-or-nothing؛ فقط درخواست‌های زیر سقف <c>BULK_APPROVE_LIMIT</c> واجد شرایط‌اند.
    /// ۴۰۹ با <c>failedIds</c> اگر حتی یکی رد شود.
    /// </summary>
    [HttpPost("payment-requests/bulk-approve")]
    [ProducesResponseType(typeof(BulkApprovePaymentRequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkApprovePaymentRequests(
        [FromBody] BulkApprovePaymentRequestsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new BulkApprovePaymentRequestsCommand(request.Ids), cancellationToken);

        return Ok(new BulkApprovePaymentRequestsResponse(request.Ids));
    }

    /// <summary>
    /// بخش ۴-ب — ثبت پرداخت واقعاً انجام‌شده در بانک (بدون یکپارچگی بانکی). سند «پرداخت» صادر و
    /// TB_PAYRECIVHEAD/DETAIL نوشته می‌شود. فقط خزانه‌دار، ≠ ثبت‌کنندهٔ درخواست، فقط از «آمادهٔ اجرا».
    /// </summary>
    [HttpPost("payment-requests/{id:guid}/execute")]
    [ProducesResponseType(typeof(ExecutePaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecutePaymentRequest(
        Guid id, [FromBody] ExecutePaymentRequestRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ExecutePaymentRequestCommand(id, request.BankReference, request.PaidDate, request.DestinationIban, request.PaymentMethod),
            cancellationToken);

        return Ok(new ExecutePaymentRequestResponse(id));
    }

    /// <summary>بخش ۴-ب — تعلیق موقت. دلیل اجباری. فقط خزانه‌دار، فقط از «آمادهٔ اجرا».</summary>
    [HttpPost("payment-requests/{id:guid}/suspend")]
    [ProducesResponseType(typeof(SuspendPaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SuspendPaymentRequest(
        Guid id, [FromBody] SuspendPaymentRequestRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SuspendPaymentRequestCommand(id, request.Reason), cancellationToken);

        return Ok(new SuspendPaymentRequestResponse(id));
    }

    /// <summary>بخش ۴-ب — رفع تعلیق. بدون بدنه. فقط خزانه‌دار، فقط از «معلق».</summary>
    [HttpPost("payment-requests/{id:guid}/resume")]
    [ProducesResponseType(typeof(ResumePaymentRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResumePaymentRequest(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResumePaymentRequestCommand(id), cancellationToken);

        return Ok(new ResumePaymentRequestResponse(id));
    }

    /// <summary>بخش ۴-ب — سند «شناسایی بدهی»/«پرداخت» (اگر صادر شده باشند) + کد Legacy PayReciv.</summary>
    [HttpGet("payment-requests/{id:guid}/accounting")]
    [ProducesResponseType(typeof(PaymentRequestAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentRequestAccounting(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentRequestAccountingQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    // ==================== کارتابل تأیید ====================

    /// <summary>
    /// ادغام درخواست‌های پرداخت Pending* و ترمیم‌های تنخواهِ PendingTreasurer در یک فهرست، به
    /// ترتیب عمر (قدیمی‌ترین اول).
    /// </summary>
    [HttpGet("approval-cartable")]
    [ProducesResponseType(typeof(PagedResult<ApprovalCartableItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApprovalCartable(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetApprovalCartableQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    // ==================== تفصیلی ذی‌نفع (اصلاح ۴-الف، ۲۰۲۶-۰۹-۲۹) ====================

    /// <summary>
    /// صفحه‌ای از تفصیلی(های) عضو گروه تفصیلی ذی‌نفعِ تعریف‌شده برای واحد — صفحهٔ خالی (نه خطا)
    /// اگر واحد هنوز گروهی تعریف نکرده باشد.
    /// </summary>
    [HttpGet("beneficiary-tafsilis")]
    [ProducesResponseType(typeof(PagedResult<TafsiliLookupItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBeneficiaryTafsilis(
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetBeneficiaryTafsilisQuery(search, pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }
}

// ==================== تنظیمات: request/response DTOs ====================

/// <summary>Request body for <see cref="TreasuryController.UpsertSettings"/>.</summary>
public sealed record UpsertTreasurySettingRequest(
    decimal CeoApprovalThreshold,
    decimal BulkApproveLimit,
    Guid? BeneficiaryTafsilGroupId,
    Guid? PayablesAccountId,
    Guid? VatCreditAccountId,
    Guid? InsurancePayableAccountId);

// ==================== نقش‌ها: request/response DTOs ====================

/// <summary>Request body for <see cref="TreasuryController.CreateRole"/>.</summary>
public sealed record CreateTreasuryRoleRequest(string UserId, string? UserName, TreasuryRole Role);

/// <summary>Response body for a successful <see cref="TreasuryController.CreateRole"/> call.</summary>
public sealed record CreateTreasuryRoleResponse(Guid Id);

/// <summary>Response body for a successful <see cref="TreasuryController.DeleteRole"/> call.</summary>
public sealed record DeleteTreasuryRoleResponse(Guid Id);

// ==================== درخواست پرداخت: request/response DTOs ====================

/// <summary>Request body for <see cref="TreasuryController.CreatePaymentRequest"/>.</summary>
public sealed record CreatePaymentRequestRequest(
    string BeneficiaryName,
    string? BeneficiaryNationalId,
    Guid? BeneficiaryTafsiliId,
    TreasuryPaymentType PaymentType,
    string? InvoiceRef,
    bool InvoiceApproved,
    Guid ExpenseAccountId,
    IReadOnlyList<PaymentRequestTafsiliLinkInput>? CostCenterTafsilis,
    decimal AmountBeforeTax,
    decimal? VatPercent,
    decimal? VatAmount,
    decimal? InsuranceDeductionPercent,
    decimal? InsuranceDeductionAmount,
    string DueDate,
    Guid PaymentAccountId,
    TreasuryPaymentMethod? PaymentMethod,
    string? Description,
    string Year,
    bool Submit);

/// <summary>Response body for a successful <see cref="TreasuryController.CreatePaymentRequest"/> call.</summary>
public sealed record CreatePaymentRequestResponse(Guid Id);

/// <summary>Request body for <see cref="TreasuryController.UpdatePaymentRequest"/>. <c>Id</c> is taken from the route.</summary>
public sealed record UpdatePaymentRequestRequest(
    string BeneficiaryName,
    string? BeneficiaryNationalId,
    Guid? BeneficiaryTafsiliId,
    TreasuryPaymentType PaymentType,
    string? InvoiceRef,
    bool InvoiceApproved,
    Guid ExpenseAccountId,
    IReadOnlyList<PaymentRequestTafsiliLinkInput>? CostCenterTafsilis,
    decimal AmountBeforeTax,
    decimal? VatPercent,
    decimal? VatAmount,
    decimal? InsuranceDeductionPercent,
    decimal? InsuranceDeductionAmount,
    string DueDate,
    Guid PaymentAccountId,
    TreasuryPaymentMethod? PaymentMethod,
    string? Description);

/// <summary>Response body for a successful <see cref="TreasuryController.UpdatePaymentRequest"/> call.</summary>
public sealed record UpdatePaymentRequestResponse(Guid Id);

/// <summary>Response body for a successful <see cref="TreasuryController.DeletePaymentRequest"/> call.</summary>
public sealed record DeletePaymentRequestResponse(Guid Id);

/// <summary>Response body for a successful <see cref="TreasuryController.SubmitPaymentRequest"/> call.</summary>
public sealed record SubmitPaymentRequestResponse(Guid Id);

/// <summary>Request body for <see cref="TreasuryController.ApprovePaymentRequest"/>. May be omitted entirely (no note).</summary>
public sealed record ApprovePaymentRequestRequest(string? Note);

/// <summary>Response body for a successful <see cref="TreasuryController.ApprovePaymentRequest"/> call.</summary>
public sealed record ApprovePaymentRequestResponse(Guid Id);

/// <summary>Request body for <see cref="TreasuryController.ReturnPaymentRequest"/>.</summary>
public sealed record ReturnPaymentRequestRequest(string Reason);

/// <summary>Response body for a successful <see cref="TreasuryController.ReturnPaymentRequest"/> call.</summary>
public sealed record ReturnPaymentRequestResponse(Guid Id);

/// <summary>Request body for <see cref="TreasuryController.RejectPaymentRequest"/>.</summary>
public sealed record RejectPaymentRequestRequest(string Reason);

/// <summary>Response body for a successful <see cref="TreasuryController.RejectPaymentRequest"/> call.</summary>
public sealed record RejectPaymentRequestResponse(Guid Id);

/// <summary>Request body for <see cref="TreasuryController.BulkApprovePaymentRequests"/>.</summary>
public sealed record BulkApprovePaymentRequestsRequest(IReadOnlyList<Guid> Ids);

/// <summary>Response body for a successful (all-or-nothing) <see cref="TreasuryController.BulkApprovePaymentRequests"/> call.</summary>
public sealed record BulkApprovePaymentRequestsResponse(IReadOnlyList<Guid> Ids);

// ==================== بخش ۴-ب: اجرای پرداخت — request/response DTOs ====================

/// <summary>Request body for <see cref="TreasuryController.ExecutePaymentRequest"/>.</summary>
public sealed record ExecutePaymentRequestRequest(
    string BankReference, string PaidDate, string? DestinationIban, TreasuryPaymentMethod? PaymentMethod);

/// <summary>Response body for a successful <see cref="TreasuryController.ExecutePaymentRequest"/> call.</summary>
public sealed record ExecutePaymentRequestResponse(Guid Id);

/// <summary>Request body for <see cref="TreasuryController.SuspendPaymentRequest"/>.</summary>
public sealed record SuspendPaymentRequestRequest(string Reason);

/// <summary>Response body for a successful <see cref="TreasuryController.SuspendPaymentRequest"/> call.</summary>
public sealed record SuspendPaymentRequestResponse(Guid Id);

/// <summary>Response body for a successful <see cref="TreasuryController.ResumePaymentRequest"/> call.</summary>
public sealed record ResumePaymentRequestResponse(Guid Id);
