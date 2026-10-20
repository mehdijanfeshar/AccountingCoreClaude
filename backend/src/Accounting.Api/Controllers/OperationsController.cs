using Accounting.Application.OperationTemplates;
using Accounting.Application.OperationTemplates.History;
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

    /// <summary>پیشنهاد جواب برای یک سؤال تفصیلی: پنج مقدار پرکاربرد این واحد در همین الگو با تعداد.</summary>
    [HttpGet("templates/{id:guid}/suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<AnswerSuggestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suggestions(Guid id, [FromQuery] string paramKey, CancellationToken ct) =>
        Ok(await _mediator.Send(new SuggestAnswersQuery(id, paramKey ?? string.Empty), ct));

    /// <summary>پیش‌نمایش سند — بدون ثبت</summary>
    [HttpPost("{templateId:guid}/preview")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Preview(Guid templateId, [FromBody] OperationInput input, [FromQuery] string? year, CancellationToken ct)
    {
        var r = await _mediator.Send(new PreviewOperationQuery(templateId, input.VoucherDate, input.Values ?? new(),
            input.AnswerSources, input.AiConfirmed, input.HasAttachment) { Year = year }, ct);
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
            templateId, input.VoucherDate, input.Values ?? new(), input.ClientRequestId, AiTraceId: input.AiTraceId,
            UserSentence: input.UserSentence, AnswerSources: input.AnswerSources, AiConfirmed: input.AiConfirmed,
            HasAttachment: input.HasAttachment) { Year = year }, ct);
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

    // ─────────── سندهای پرتکرار (DDL 079) — ثبت از همان preview/execute الگو ───────────

    /// <summary>سندهای پرتکرار واحد جاری با جواب‌های ثابت.</summary>
    [HttpGet("quick")]
    [ProducesResponseType(typeof(IReadOnlyList<QuickVoucherDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> QuickVouchers(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListQuickVouchersQuery(), ct));

    /// <summary>تعریف سند پرتکرار: الگو + جواب‌های ثابت (بقیه هنگام ثبت پرسیده می‌شود).</summary>
    [HttpPost("quick")]
    [ProducesResponseType(typeof(QuickVoucherWriteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateQuickVoucher([FromBody] CreateQuickVoucherCommand cmd, CancellationToken ct)
    {
        var r = await _mediator.Send(cmd, ct);
        return r.Success ? Ok(r) : Problem422("سند پرتکرار ذخیره نشد.", r.Errors);
    }

    [HttpPost("quick/{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuickVoucher(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteQuickVoucherCommand { Id = id }, ct);
        return NoContent();
    }

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

    // ─────────── فاز ۶۳: یادگیری از سوابق (DDL 085) ───────────
    // جدول سوابق خالی/ساخته‌نشده ⇒ ۵۰۳ ProblemDetails «جدول سوابق ساخته نشده؛ مدیر ستاد «بازسازی» را بزند».

    /// <summary>بازسازی جدول سوابق از سندهای دائم/بررسی‌شدهٔ بازهٔ سال — فقط مدیر ستاد مرکزی. سنگین است؛ کم‌بار اجرا شود.</summary>
    [HttpPost("history/patterns/rebuild")]
    [ProducesResponseType(typeof(HistoryRebuildResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RebuildHistory([FromBody] RebuildHistoryInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new RebuildHistoryPatternsCommand(input.FromYear, input.ToYear, input.RestoreDismissed), ct));

    /// <summary>الگوهای پرتکرار همهٔ واحدها (کشف) — ستاد و نقش مدیریتی کشور (فقط‌خواندنی).</summary>
    [HttpGet("history/patterns")]
    [ProducesResponseType(typeof(HistoryPatternPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HistoryPatterns([FromQuery] int? fromYear, [FromQuery] int? toYear, [FromQuery] string? vahedType,
        [FromQuery] int minCount = 0, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new ListHistoryPatternsQuery(fromYear, toYear, vahedType, minCount, search, page, pageSize), ct));

    /// <summary>حذف گروهی همهٔ پیشنهادهای مطابق فیلتر که به الگو تبدیل نشده‌اند (ماندگار). فیلتر خالی = همه.</summary>
    [HttpPost("history/patterns/delete-uncovered")]
    [ProducesResponseType(typeof(HistoryBulkDeleteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteUncoveredHistoryPatterns([FromBody] DeleteUncoveredHistoryInput? input, CancellationToken ct) =>
        Ok(await _mediator.Send(new DeleteUncoveredHistoryPatternsCommand(input?.FromYear, input?.ToYear, input?.VahedType, input?.MinCount ?? 0, input?.Search), ct));

    /// <summary>حذف پیشنهاد کشف‌شده (ماندگار؛ بازسازی دوباره نمی‌سازدش). فقط وقتی به الگو تبدیل نشده، وگرنه ۴۰۹.</summary>
    [HttpPost("history/patterns/{id:guid}/delete")]
    [ProducesResponseType(typeof(HistoryDeleteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteHistoryPattern(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new DeleteHistoryPatternCommand(id), ct));

    /// <summary>تعریف الگوی ذخیره‌نشده از یک الگوی سابقه برای پیش‌پر کردن طراح الگو (همان شکل بدنهٔ ساخت الگو).</summary>
    [HttpPost("history/patterns/{id:guid}/to-template")]
    [ProducesResponseType(typeof(HistoryTemplateDraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HistoryPatternToTemplate(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new HistoryPatternToTemplateQuery(id), ct));

    /// <summary>سه الگوی سابقهٔ نزدیک به جمله (فقط حساب‌ها و ساختار؛ هرگز تفصیلی واحدهای دیگر). واحد از هدر؛ سال از query.</summary>
    [HttpPost("history/match")]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryMatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HistoryMatch([FromBody] HistoryMatchInput input, [FromQuery] string? year, CancellationToken ct) =>
        Ok(await _mediator.Send(new MatchHistoryPatternsQuery(input.Sentence ?? string.Empty) { Year = year }, ct));

    /// <summary>پیش‌نمایش سند از روی سابقه — بدون ثبت. حساب‌ها را سرور از سابقه می‌سازد.</summary>
    [HttpPost("history/{patternId:guid}/preview")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HistoryPreview(Guid patternId, [FromBody] HistoryOperationInput input, [FromQuery] string? year, CancellationToken ct) =>
        ToHttp(await _mediator.Send(new PreviewHistoryOperationQuery(patternId, input.VoucherDate, input.MergedAnswers(),
            input.Description, input.UserSentence, input.AnswerSources, input.AiConfirmed, input.HasAttachment, input.LineExtras) { Year = year }, ct));

    /// <summary>ثبت سند (موقت) از روی سابقه؛ کانال History، ریسک بالا و ورود به صف حسابدار.</summary>
    [HttpPost("history/{patternId:guid}/execute")]
    [ProducesResponseType(typeof(OperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HistoryExecute(Guid patternId, [FromBody] HistoryExecuteInput input, [FromQuery] string? year, CancellationToken ct) =>
        ToHttp(await _mediator.Send(new ExecuteHistoryOperationCommand(patternId, input.VoucherDate, input.MergedAnswers(), input.ClientRequestId,
            input.Description, input.UserSentence, input.AnswerSources, input.AiConfirmed, input.HasAttachment, input.AiTraceId, input.LineExtras) { Year = year }, ct));

    // ─────────── فاز ۶۱: صف بررسی و «سندهای من» (DDL 081) ───────────

    /// <summary>
    /// صف بررسی: سندهای «موقت» حسابیار واحد جاری (قدیمی‌ترین اول) — فقط مسئول حسابداری و مدیران واحد.
    /// <c>risk</c>: <c>low</c> | <c>attention</c> (متوسط و بالا) | خالی = همه. پاسخ شمارنده‌های <c>counts</c> {all, low, attention} را هم دارد.
    /// </summary>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ReviewQueuePageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReviewQueue(
        [FromQuery] string? risk, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? year = null, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new ListReviewQueueQuery(risk, page, pageSize) { Year = year }, ct));

    /// <summary>جزئیات یک سند در صف همراه تاریخچهٔ بررسی/پاسخ‌ها (فقط بررسی‌کنندگان).</summary>
    [HttpGet("reviews/{executionId:guid}")]
    [ProducesResponseType(typeof(ReviewDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewDetail(Guid executionId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetReviewDetailQuery(executionId), ct));

    /// <summary>«بررسی شد» — همان تغییر وضعیت به «بررسی‌شده»؛ ثبت‌کنندهٔ همان سند ۴۰۳ می‌گیرد (G1).</summary>
    [HttpPost("reviews/{executionId:guid}/approve")]
    [ProducesResponseType(typeof(ReviewActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveReview(Guid executionId, CancellationToken ct) =>
        Ok(await _mediator.Send(new ApproveReviewsCommand([executionId]), ct));

    /// <summary>«بررسی شد» دسته‌ای — همه یا هیچ (یک سند ناممکن ⇒ کل دسته رد می‌شود).</summary>
    [HttpPost("reviews/approve-batch")]
    [ProducesResponseType(typeof(ReviewActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveReviewBatch([FromBody] ApproveReviewBatchInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new ApproveReviewsCommand(input.ExecutionIds ?? []), ct));

    /// <summary>برگشت با دلیل: سند به «یادداشت» برمی‌گردد و پیام برای ثبت‌کننده می‌ماند.</summary>
    [HttpPost("reviews/{executionId:guid}/return")]
    [ProducesResponseType(typeof(ReviewActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReturnReview(Guid executionId, [FromBody] ReturnReviewInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new ReturnReviewCommand(executionId, input.ReasonCode, input.Message), ct));

    /// <summary>سندهای حسابیار کاربر جاری با شمارندهٔ وضعیت‌ها، آخرین پیام برگشت و قدیمی‌ترین منتظر بررسی.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(MineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine([FromQuery] string? status, [FromQuery] string? year, CancellationToken ct) =>
        Ok(await _mediator.Send(new ListMyVouchersQuery(status) { Year = year }, ct));

    /// <summary>«پاسخ به حسابدار» — فقط ثبت‌کنندهٔ همان سند (۴۰۳ برای دیگران)؛ در تاریخچه عمل Replied می‌شود.</summary>
    [HttpPost("mine/{executionId:guid}/reply")]
    [ProducesResponseType(typeof(ReviewActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reply(Guid executionId, [FromBody] ReplyInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new ReplyToReviewCommand(executionId, input.Message), ct));

    // ─────────── پیوست مدرک سند حسابیار (TB_ATTACH) ───────────

    /// <summary>پیوست‌های یک سند حسابیار (بدون محتوا) — ثبت‌کننده یا بررسی‌کننده.</summary>
    [HttpGet("vouchers/{voucherId:guid}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<VoucherAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Attachments(Guid voucherId, CancellationToken ct) =>
        Ok(await _mediator.Send(new ListVoucherAttachmentsQuery(voucherId), ct));

    /// <summary>افزودن مدرک — فقط ثبت‌کنندهٔ سند و فقط تا سند «یادداشت» یا «موقت» است (۴۰۹ پس از آن).</summary>
    [HttpPost("vouchers/{voucherId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(Accounting.Api.Security.UploadGuard.AttachmentRequestLimit)]
    [ProducesResponseType(typeof(UploadVoucherAttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadAttachment(Guid voucherId, [FromForm] UploadVoucherAttachmentRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "فایل پیوست الزامی است.");
            return ValidationProblem(ModelState);
        }

        var fileError = await Accounting.Api.Security.UploadGuard.ValidateAttachmentAsync(
            request.File, VoucherAttachmentAccessLimits.MaxFileBytes, ct);
        if (fileError is not null)
        {
            ModelState.AddModelError(nameof(request.File), fileError);
            return ValidationProblem(ModelState);
        }

        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, ct);
        var name = Accounting.Api.Security.UploadGuard.SafeFileName(request.File.FileName);
        var id = await _mediator.Send(new UploadVoucherAttachmentCommand(voucherId, name, request.Title, stream.ToArray()), ct);
        return Ok(new UploadVoucherAttachmentResponse(id));
    }

    /// <summary>دریافت فایل پیوست (همیشه دانلود، نه نمایش در مرورگر).</summary>
    [HttpGet("vouchers/{voucherId:guid}/attachments/{attachmentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(Guid voucherId, Guid attachmentId, CancellationToken ct)
    {
        var file = await _mediator.Send(new GetVoucherAttachmentFileQuery(voucherId, attachmentId), ct);
        return File(file.Content, "application/octet-stream", file.FileName);
    }

    /// <summary>حذف نرم پیوست — همان قاعدهٔ افزودن.</summary>
    [HttpPost("vouchers/{voucherId:guid}/attachments/{attachmentId:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAttachment(Guid voucherId, Guid attachmentId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteVoucherAttachmentCommand(voucherId, attachmentId), ct);
        return NoContent();
    }

    // ─────────── «موضوع را برای حسابدار بفرستید» (DDL 084) ───────────

    /// <summary>ارسال جمله برای حسابدار واحد (هر کاربر عملیات).</summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(CreateOperationRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequest([FromBody] RequestInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new CreateOperationRequestCommand(input.Sentence), ct));

    /// <summary>درخواست‌های واحد (فقط بررسی‌کنندگان). status: open | done | خالی = همه.</summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(IReadOnlyList<OperationRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Requests([FromQuery] string? status, CancellationToken ct) =>
        Ok(await _mediator.Send(new ListOperationRequestsQuery(status), ct));

    [HttpPost("requests/{id:guid}/done")]
    [ProducesResponseType(typeof(ReviewActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestDone(Guid id, [FromBody] RequestDoneInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new MarkRequestDoneCommand(id, input?.Note), ct));

    /// <summary>ردپای هوش مصنوعی (G4) — فقط نقش حسابرسی. <c>executionId</c> خالی = آخرین ۱۰۰ ردپای واحد.</summary>
    [HttpGet("ai-traces")]
    [ProducesResponseType(typeof(IReadOnlyList<AiTraceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AiTraces([FromQuery] Guid? executionId, CancellationToken ct) =>
        Ok(await _mediator.Send(new ListAiTracesQuery(executionId), ct));

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

/// <param name="AnswerSources">منبع هر جواب: sentence | ai | recent | quick | user (اختیاری).</param>
/// <param name="AiConfirmed">کاربر جواب‌های پرشدهٔ هوش مصنوعی را صریحاً تأیید کرده است.</param>
/// <param name="HasAttachment">مدرک پیوست شده است (سند پیوست ندارد تا ثبت؛ فرانت اعلام می‌کند).</param>
/// <param name="UserSentence">جملهٔ کامل کاربر (حداکثر ۱۰۰۰)؛ فقط در Execute ذخیره می‌شود.</param>
public record OperationInput(DateOnly? VoucherDate, Dictionary<string, string?>? Values,
    Dictionary<string, string?>? AnswerSources = null, bool AiConfirmed = false, bool HasAttachment = false, string? UserSentence = null);

public sealed record ExecuteInput(DateOnly? VoucherDate, Dictionary<string, string?>? Values, Guid ClientRequestId, Guid? AiTraceId = null,
    Dictionary<string, string?>? AnswerSources = null, bool AiConfirmed = false, bool HasAttachment = false, string? UserSentence = null)
    : OperationInput(VoucherDate, Values, AnswerSources, AiConfirmed, HasAttachment, UserSentence);

public sealed record RebuildHistoryInput(int FromYear, int ToYear, bool RestoreDismissed = false);

public sealed record DeleteUncoveredHistoryInput(int? FromYear = null, int? ToYear = null, string? VahedType = null, int MinCount = 0, string? Search = null);

public sealed record HistoryMatchInput(string? Sentence);

/// <summary>
/// بدنهٔ پیش‌نمایش سابقه — همان شکل الگوها. جواب‌ها در <c>answers</c> (یا هم‌نام قدیمی <c>values</c>)؛ شناسهٔ حساب هرگز پذیرفته نمی‌شود.
/// </summary>
public record HistoryOperationInput(DateOnly? VoucherDate, Dictionary<string, string?>? Answers = null, Dictionary<string, string?>? Values = null,
    string? Description = null, string? UserSentence = null, Dictionary<string, string?>? AnswerSources = null,
    bool AiConfirmed = false, bool HasAttachment = false, Guid? AiTraceId = null,
    IReadOnlyList<DraftLineExtrasInput>? LineExtras = null)
{
    public Dictionary<string, string?> MergedAnswers()
    {
        var merged = new Dictionary<string, string?>(Values ?? new(), StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in Answers ?? new()) merged[k] = v;
        return merged;
    }
}

public sealed record HistoryExecuteInput(DateOnly? VoucherDate, Guid ClientRequestId, Dictionary<string, string?>? Answers = null,
    Dictionary<string, string?>? Values = null, string? Description = null, string? UserSentence = null,
    Dictionary<string, string?>? AnswerSources = null, bool AiConfirmed = false, bool HasAttachment = false, Guid? AiTraceId = null,
    IReadOnlyList<DraftLineExtrasInput>? LineExtras = null)
    : HistoryOperationInput(VoucherDate, Answers, Values, Description, UserSentence, AnswerSources, AiConfirmed, HasAttachment, AiTraceId, LineExtras);

public sealed record ApproveReviewBatchInput(List<Guid>? ExecutionIds);

/// <summary>بدنهٔ multipart افزودن پیوست سند حسابیار.</summary>
public sealed class UploadVoucherAttachmentRequest
{
    public IFormFile File { get; set; } = null!;

    /// <summary>عنوان اختیاری (مثلاً «فاکتور خرید»).</summary>
    public string? Title { get; set; }
}

public sealed record UploadVoucherAttachmentResponse(Guid Id);
