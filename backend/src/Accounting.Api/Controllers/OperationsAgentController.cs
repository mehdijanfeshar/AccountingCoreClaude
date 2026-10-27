using Accounting.Application.OperationTemplates.Agent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// حسابیار فاز ۶۵ — گفتگو با دستیار (DDL 087) و سنجش هوش مصنوعی (DDL 088). واحد فقط از هدر
/// <c>X-Vahed-Code</c>/توکن؛ <c>year</c> از query. دستیار هیچ‌وقت سند ثبت نمی‌کند — فقط پیشنهاد سند می‌دهد و ثبت
/// با همان <c>POST api/operations/{templateId}/execute</c> و کلیک کاربر است.
/// </summary>
[ApiController]
[Route("api/operations")]
public sealed class OperationsAgentController : ControllerBase
{
    private readonly IMediator _mediator;
    public OperationsAgentController(IMediator mediator) => _mediator = mediator;

    // ─────────── گفتگو ───────────

    /// <summary>روشن بودن دستیار و فعال بودن ابزارهای دادهٔ حساب.</summary>
    [HttpGet("agent/status")]
    [ProducesResponseType(typeof(AgentStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAgentStatusQuery(), ct));

    /// <summary>
    /// یک پیام کاربر ⇒ پاسخ دستیار، قدم‌های کار و (اگر شد) پیشنهاد سند. <c>conversationId</c> خالی = گفتگوی تازه.
    /// هوش مصنوعی خاموش ⇒ <c>enabled=false</c>.
    /// </summary>
    [HttpPost("agent/messages")]
    [ProducesResponseType(typeof(AgentChatResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Send([FromBody] AgentMessageInput input, [FromQuery] string? year, CancellationToken ct) =>
        Ok(await _mediator.Send(new SendAgentMessageCommand(input.ConversationId, input.Message ?? string.Empty) { Year = year }, ct));

    /// <summary>گفتگوهای کاربر در واحد جاری (۳۰ تای آخر).</summary>
    [HttpGet("agent/conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<AgentConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Conversations(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListAgentConversationsQuery(), ct));

    /// <summary>پیام‌های یک گفتگو (فقط کاربر و دستیار) با پیشنهادهای سند.</summary>
    [HttpGet("agent/conversations/{id:guid}")]
    [ProducesResponseType(typeof(AgentConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Conversation(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAgentConversationQuery(id), ct));

    /// <summary>حذف نرم گفتگو.</summary>
    [HttpPost("agent/conversations/{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteAgentConversationCommand(id), ct);
        return NoContent();
    }

    // ─────────── سنجش هوش مصنوعی (قابلیت ai.evaluate) ───────────

    [HttpGet("ai-eval/golden")]
    [ProducesResponseType(typeof(IReadOnlyList<AiGoldenCaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Golden(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListAiGoldenCasesQuery(), ct));

    [HttpPost("ai-eval/golden")]
    [ProducesResponseType(typeof(AiGoldenCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGolden([FromBody] AiGoldenCaseInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(input.ToCommand(null), ct));

    [HttpPost("ai-eval/golden/{id:guid}/update")]
    [ProducesResponseType(typeof(AiGoldenCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGolden(Guid id, [FromBody] AiGoldenCaseInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(input.ToCommand(id), ct));

    [HttpPost("ai-eval/golden/{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGolden(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteAiGoldenCaseCommand(id), ct);
        return NoContent();
    }

    /// <summary>ساخت نمونهٔ طلایی از اسناد واقعی حسابیار که جملهٔ کاربر دارند. پاسخ = تعداد نمونهٔ تازه.</summary>
    [HttpPost("ai-eval/golden/import")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportGolden([FromBody] AiGoldenImportInput input, CancellationToken ct) =>
        Ok(await _mediator.Send(new ImportAiGoldenCasesCommand(input?.Take ?? 50, input?.AllUnits ?? false), ct));

    /// <summary>یک دور سنجش: <c>kind</c> = golden | shadow. همزمان (حداکثر ۵۰ جمله یا ~۹۰ ثانیه).</summary>
    [HttpPost("ai-eval/runs")]
    [ProducesResponseType(typeof(AiEvalRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Run([FromBody] AiEvalRunInput input, [FromQuery] string? year, CancellationToken ct) =>
        Ok(await _mediator.Send(new RunAiEvaluationCommand(input.Kind ?? "golden", input.Take ?? 20, input.AllUnits ?? false) { Year = year }, ct));

    [HttpGet("ai-eval/runs")]
    [ProducesResponseType(typeof(IReadOnlyList<AiEvalRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Runs(CancellationToken ct) =>
        Ok(await _mediator.Send(new ListAiEvalRunsQuery(), ct));

    [HttpGet("ai-eval/runs/{id:guid}")]
    [ProducesResponseType(typeof(AiEvalRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunDetail(Guid id, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetAiEvalRunQuery(id), ct));
}

public sealed record AgentMessageInput(Guid? ConversationId, string? Message);

public sealed record AiGoldenCaseInput(string? Sentence, string? ExpectedTemplateCode, Dictionary<string, string>? ExpectedAnswers, string? Note, bool IsActive = true)
{
    public SaveAiGoldenCaseCommand ToCommand(Guid? id) => new(id, Sentence ?? string.Empty, ExpectedTemplateCode, ExpectedAnswers, Note, IsActive);
}

public sealed record AiGoldenImportInput(int? Take, bool? AllUnits);

public sealed record AiEvalRunInput(string? Kind, int? Take, bool? AllUnits);
