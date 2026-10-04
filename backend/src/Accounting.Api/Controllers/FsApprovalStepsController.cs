using Accounting.Application.FinancialStatements.Approvals;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، ح-۴ — مراحل گردش تأیید (<c>docs/fs-module.md</c> §۱۳). Vahed-scoped؛ مالکیت مثل قواعد
/// کنترل: مشترک فقط ستاد (۴۰۳)، اختصاصی خود/زیرمجموعه. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs/approval-steps")]
public sealed class FsApprovalStepsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsApprovalStepsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FsApprovalStepDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList([FromQuery] FsFramework? framework, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsApprovalStepsQuery(framework), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateFsApprovalStepCommand command, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFsApprovalStepRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateFsApprovalStepCommand(id, request.StepNo, request.TitleFa, request.ApproverUserIds, request.IsActive),
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
        await _mediator.Send(new DeleteFsApprovalStepCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }
}

public sealed record UpdateFsApprovalStepRequest(int StepNo, string TitleFa, string? ApproverUserIds, bool IsActive);
