using Accounting.Application.FinancialStatements.CheckRules;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، بخش ۴۵-ه — قواعد کنترل تساوی بین صورت‌ها (<c>docs/fs-module.md</c> §۱۱). Vahed-scoped؛
/// مالکیت مثل قالب‌ها: مشترک فقط ستاد (۴۰۳)، اختصاصی خود/زیرمجموعه. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs/check-rules")]
public sealed class FsCheckRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsCheckRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FsCheckRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList([FromQuery] FsFramework? framework, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsCheckRulesQuery(framework), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateFsCheckRuleCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new FsIdResponse(id));
    }

    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFsCheckRuleRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateFsCheckRuleCommand(id, request.TitleFa, request.LeftExpr, request.RightExpr, request.Tolerance, request.Severity, request.IsActive),
            cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsCheckRuleCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }
}

public sealed record UpdateFsCheckRuleRequest(
    string TitleFa,
    string LeftExpr,
    string RightExpr,
    decimal Tolerance,
    FsCheckSeverity Severity,
    bool IsActive);
