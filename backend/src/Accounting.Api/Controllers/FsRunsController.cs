using Accounting.Application.FinancialStatements.Commands.DeleteFsRun;
using Accounting.Application.FinancialStatements.Commands.GenerateFsRun;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Application.FinancialStatements.Queries.GetFsRun;
using Accounting.Application.FinancialStatements.Queries.GetFsRuns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، بخش ۴۵-ب — تهیهٔ صورت‌ها (اجرا) و خواندن Snapshot
/// (<c>docs/fs-module.md</c> §۷). همه Vahed-scoped: واحد فقط از هدر <c>X-Vahed-Code</c> (قاعدهٔ ۴)؛
/// اجرای واحد دیگر ۴۰۴ است. فقط GET/POST.
/// <list type="bullet">
/// <item>۴۰۰: ورودی نامعتبر.</item>
/// <item>۴۰۳: هدر واحد خارج از دسترس کاربر.</item>
/// <item>۴۰۹: مجموعه قالب قابل‌استفاده ندارد، یا قالب قابل محاسبه نیست (ارجاع/دور بین صورت‌ها).</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/fs/runs")]
public sealed class FsRunsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsRunsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FsRunSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList([FromQuery] string? year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsRunsQuery(year), cancellationToken));

    /// <summary>تهیهٔ صورت‌ها (هم‌زمان). پاسخ ۲۰۱ با شناسهٔ اجرا.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate([FromBody] GenerateFsRunCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new FsIdResponse(id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FsRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsRunQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsRunCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }
}
