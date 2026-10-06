using Accounting.Application.OperationTemplates;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Agent-UX فاز ۱ — عملیات از روی الگو. واحد فقط از هدر <c>X-Vahed-Code</c>/توکن (VahedScopeBehavior)؛
/// نقش‌ها با RoleAuthorizationBehavior (ماژول OperationTemplates) و تعریف الگو فقط «مدیر ستاد».
/// خطای موتور ⇒ ProblemDetails با status 422 و فهرست <c>errors</c> (کد، پیام، parameterKey، askPrompt).
/// </summary>
[ApiController]
[Route("api/operations")]
public sealed class OperationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public OperationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست عملیات قابل انجام + پارامترهای هرکدام (برای ساخت فرم پویا)</summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListOperationTemplatesQuery(), ct));

    /// <summary>تعریف الگوی جدید — فقط مدیر ستاد</summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(CreateTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateOperationTemplateCommand cmd, CancellationToken ct)
    {
        var r = await _mediator.Send(cmd, ct);
        return r.Success ? Ok(r) : Problem422("تعریف الگو معتبر نیست.", r.Errors);
    }

    /// <summary>طراحی الگو: همهٔ الگوها (فعال و غیرفعال)</summary>
    [HttpGet("templates/definitions")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateDefinitionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDefinitions(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListOperationTemplateDefinitionsQuery(), ct));

    /// <summary>طراحی الگو: تعریف کامل یک الگو</summary>
    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(TemplateDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefinition(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetOperationTemplateDefinitionQuery(id), ct));

    /// <summary>طراحی الگو: بررسی بدون ذخیره</summary>
    [HttpPost("templates/validate")]
    [ProducesResponseType(typeof(CreateTemplateResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateDefinition([FromBody] ValidateOperationTemplateQuery query, CancellationToken ct) =>
        Ok(await _mediator.Send(query, ct));

    /// <summary>طراحی الگو: ویرایش (جایگزینی کامل) — فقط مدیر ستاد</summary>
    [HttpPost("templates/{id:guid}/update")]
    [ProducesResponseType(typeof(CreateTemplateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] UpdateOperationTemplateCommand cmd, CancellationToken ct)
    {
        cmd.Id = id;
        var r = await _mediator.Send(cmd, ct);
        return r.Success ? Ok(r) : Problem422("تعریف الگو معتبر نیست.", r.Errors);
    }

    /// <summary>طراحی الگو: فعال/غیرفعال — فقط مدیر ستاد</summary>
    [HttpPost("templates/{id:guid}/set-active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetOperationTemplateActiveCommand cmd, CancellationToken ct)
    {
        cmd.Id = id;
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    /// <summary>پیش‌نمایش سند — بدون ثبت</summary>
    [HttpPost("{templateId:guid}/preview")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Preview(Guid templateId, [FromBody] OperationInput input, [FromQuery] string? year, CancellationToken ct)
    {
        var r = await _mediator.Send(new PreviewOperationQuery(templateId, input.VoucherDate, input.Values ?? new()) { Year = year }, ct);
        return ToHttp(r);
    }

    /// <summary>ثبت سند (موقت). ClientRequestId را فرانت یک بار برای هر فرم می‌سازد.</summary>
    [HttpPost("{templateId:guid}/execute")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Execute(Guid templateId, [FromBody] ExecuteInput input, [FromQuery] string? year, CancellationToken ct)
    {
        var r = await _mediator.Send(new ExecuteOperationCommand(
            templateId, input.VoucherDate, input.Values ?? new(), input.ClientRequestId) { Year = year }, ct);
        return ToHttp(r);
    }

    // ─────────── گزارش‌ساز حسابیار — گزارش‌های ذخیره‌شده (DDL 072) ───────────

    /// <summary>گزارش‌های ذخیره‌شدهٔ فعال که برای نوع واحد کاربر مجازند.</summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SavedReports(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListSavedReportsQuery(), ct));

    /// <summary>همهٔ گزارش‌های ذخیره‌شده (فعال و غیرفعال) — صفحهٔ مدیریت.</summary>
    [HttpGet("reports/definitions")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SavedReportDefinitions(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListSavedReportDefinitionsQuery(), ct));

    [HttpGet("reports/{id:guid}")]
    [ProducesResponseType(typeof(SavedReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SavedReport(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetSavedReportQuery(id), ct));

    /// <summary>گزارش ذخیره‌شدهٔ جدید — فقط مدیر ستاد.</summary>
    [HttpPost("reports")]
    [ProducesResponseType(typeof(SavedReportWriteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSavedReport([FromBody] CreateSavedReportCommand cmd, CancellationToken ct)
    {
        var r = await _mediator.Send(cmd, ct);
        return r.Success ? Ok(r) : Problem422("تعریف گزارش معتبر نیست.", r.Errors);
    }

    [HttpPost("reports/{id:guid}/update")]
    [ProducesResponseType(typeof(SavedReportWriteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSavedReport(Guid id, [FromBody] UpdateSavedReportCommand cmd, CancellationToken ct)
    {
        var r = await _mediator.Send(cmd with { Id = id }, ct);
        return r.Success ? Ok(r) : Problem422("تعریف گزارش معتبر نیست.", r.Errors);
    }

    [HttpPost("reports/{id:guid}/set-active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSavedReportActive(Guid id, [FromBody] SetSavedReportActiveCommand cmd, CancellationToken ct)
    {
        await _mediator.Send(cmd with { Id = id }, ct);
        return NoContent();
    }

    /// <summary>آخرین اجرای هر الگو توسط کاربر جاری در واحدش — «عملیات‌های اخیر من» و «تکرار».</summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentOperationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recent([FromQuery] int take = 8, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new ListMyRecentOperationsQuery(take), ct));

    /// <summary>سندهای حسابیار واحد جاری (تاریخ شمسی yyyyMMdd، الگو، نوع ثبت، فقط من).</summary>
    [HttpGet("executions")]
    [ProducesResponseType(typeof(ExecutionPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Executions(
        [FromQuery] string? fromDate, [FromQuery] string? toDate, [FromQuery] Guid? templateId, [FromQuery] string? channel,
        [FromQuery] bool mineOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new ListOperationExecutionsQuery(fromDate, toDate, templateId, channel, mineOnly, page, pageSize), ct));

    /// <summary>تعداد و آخرین استفادهٔ هر الگو (مدیر ستاد: همهٔ واحدها).</summary>
    [HttpGet("templates/usage")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateUsageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Usage(CancellationToken ct) =>
        Ok(await _mediator.Send(new OperationTemplateUsageQuery(), ct));

    /// <summary>«آزمایش الگو»: تعریف (ذخیره‌نشده) + جواب نمونه ⇒ پیش‌نویس سند. هیچ چیز ذخیره نمی‌شود.</summary>
    [HttpPost("templates/test")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestTemplate([FromBody] TestOperationTemplateQuery query, CancellationToken ct) =>
        Ok(await _mediator.Send(query, ct));

    /// <summary>
    /// فهم جملهٔ کاربر با هوش مصنوعی (فاز ۲): کدام الگو + جواب سؤال‌ها. بدون نوشتن. اگر هوش مصنوعی در
    /// تنظیمات سرور خاموش باشد <c>enabled=false</c> برمی‌گردد و فرانت با روش قاعده‌ای ادامه می‌دهد.
    /// </summary>
    [HttpPost("interpret")]
    [ProducesResponseType(typeof(Accounting.Application.OperationTemplates.Agent.InterpretResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Interpret(
        [FromBody] Accounting.Application.OperationTemplates.Agent.InterpretSentenceQuery query, [FromQuery] string? year, CancellationToken ct) =>
        Ok(await _mediator.Send(query with { Year = year }, ct));

    /// <summary>پیش‌نمایش سند کامل (ویرایش‌شده یا آزاد) — بدون ثبت</summary>
    [HttpPost("compose/preview")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ComposePreview([FromBody] PreviewComposedVoucherQuery query, [FromQuery] string? year, CancellationToken ct) =>
        ToHttp(await _mediator.Send(query with { Year = year }, ct));

    /// <summary>ثبت سند کامل (موقت)، اتمیک با ردپای اجرا. sourceTemplateId فقط برای ردپاست.</summary>
    [HttpPost("compose/execute")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ComposeExecute([FromBody] ExecuteComposedVoucherCommand command, [FromQuery] string? year, CancellationToken ct) =>
        ToHttp(await _mediator.Send(command with { Year = year }, ct));

    private IActionResult ToHttp(OperationResult r)
    {
        if (r.Success) return Ok(r);

        if (r.Errors.Any(e => e.Code == EngineErrorCode.TemplateNotFound))
        {
            var notFound = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "الگوی عملیات یافت نشد.",
                Instance = HttpContext.Request.Path,
            };
            return new ObjectResult(notFound) { StatusCode = notFound.Status };
        }

        // 422: ورودی کامل/درست نیست؛ errors می‌گوید چه چیزی را بپرسیم
        return Problem422("اطلاعات عملیات کامل یا معتبر نیست.", r.Errors);
    }

    private ObjectResult Problem422(string title, object errors)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = title,
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["errors"] = errors;
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}

public record OperationInput(DateOnly? VoucherDate, Dictionary<string, string?>? Values);
public sealed record ExecuteInput(DateOnly? VoucherDate, Dictionary<string, string?>? Values, Guid ClientRequestId)
    : OperationInput(VoucherDate, Values);
