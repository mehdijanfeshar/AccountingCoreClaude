using Accounting.Application.FinancialStatements.Narratives;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، ح-۶ — یادداشت‌های توضیحی متنی (<c>docs/fs-module.md</c> §۱۳). Vahed-scoped: یادداشت‌های
/// واحد هدر؛ یادداشت واحد دیگر = ۴۰۴. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs")]
public sealed class FsNarrativesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsNarrativesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("narratives")]
    [ProducesResponseType(typeof(IReadOnlyList<FsNarrativeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList([FromQuery] FsFramework framework, [FromQuery] string year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsNarrativesQuery(framework, year), cancellationToken));

    [HttpPost("narratives")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateFsNarrativeCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, new FsIdResponse(await _mediator.Send(command, cancellationToken)));

    /// <summary>ذخیرهٔ متن (نسخهٔ تازه). پاسخ = شمارهٔ نسخه. در بازبینی/تأییدشده = ۴۰۹.</summary>
    [HttpPost("narratives/{id:guid}/update")]
    [ProducesResponseType(typeof(FsNarrativeSaveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Save(Guid id, [FromBody] SaveFsNarrativeRequest request, CancellationToken cancellationToken)
    {
        var version = await _mediator.Send(
            new SaveFsNarrativeCommand(id, request.TitleFa, request.LinkedTemplateCode, request.ResponsibleUserId, request.ContentJson),
            cancellationToken);
        return Ok(new FsNarrativeSaveResponse(id, version));
    }

    [HttpPost("narratives/{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsNarrativeCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpPost("narratives/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reorder([FromBody] ReorderFsNarrativesCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary><c>{ action: 1=ارسال|2=تأیید|3=برگشت, comment }</c>. پاسخ = وضعیت تازه.</summary>
    [HttpPost("narratives/{id:guid}/transitions")]
    [ProducesResponseType(typeof(FsNarrativeTransitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transition(Guid id, [FromBody] FsNarrativeTransitionRequest request, CancellationToken cancellationToken)
        => Ok(new FsNarrativeTransitionResponse(id, await _mediator.Send(new TransitionFsNarrativeCommand(id, request.Action, request.Comment), cancellationToken)));

    /// <summary>کپی یادداشت‌های سال قبل به سال خالی. پاسخ = تعداد.</summary>
    [HttpPost("narratives/roll-forward")]
    [ProducesResponseType(typeof(FsNarrativeRollForwardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RollForward([FromBody] RollForwardFsNarrativesCommand command, CancellationToken cancellationToken)
        => Ok(new FsNarrativeRollForwardResponse(await _mediator.Send(command, cancellationToken)));

    [HttpGet("narratives/{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<FsNarrativeVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsNarrativeVersionsQuery(id), cancellationToken));

    /// <summary>یادداشت‌های متنی یک اجرا: کپی زمان انتشار، وگرنه متن جاری.</summary>
    [HttpGet("runs/{id:guid}/narratives")]
    [ProducesResponseType(typeof(IReadOnlyList<FsRunNarrativeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunNarratives(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsRunNarrativesQuery(id), cancellationToken));

    /// <summary>Word یادداشت‌ها با متغیرهای همین اجرا؛ <c>divisor</c> = واحد مبلغ (۱، ۱۰۰۰، …).</summary>
    [HttpGet("runs/{id:guid}/narratives.docx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunNarrativesDocx(Guid id, [FromQuery] decimal divisor = 1_000_000, CancellationToken cancellationToken = default)
    {
        var file = await _mediator.Send(new GetFsRunNarrativesDocxQuery(id, divisor), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }
}

public sealed record SaveFsNarrativeRequest(string TitleFa, string? LinkedTemplateCode, string? ResponsibleUserId, string? ContentJson);

public sealed record FsNarrativeSaveResponse(Guid Id, int VersionNo);

public sealed record FsNarrativeTransitionRequest(FsNarrativeAction Action, string? Comment);

public sealed record FsNarrativeTransitionResponse(Guid Id, FsNarrativeState State);

public sealed record FsNarrativeRollForwardResponse(int Count);
