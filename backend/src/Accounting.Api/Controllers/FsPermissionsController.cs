using Accounting.Application.FinancialStatements.Access;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، ط-۲ — دسترسی سه‌بُعدی (کاربر × دامنهٔ واحد × عملیات). Vahed-scoped؛ فقط «مدیریت دسترسی»
/// (یا هیچ ردیف تعریف‌نشده) تغییر می‌دهد. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs/permissions")]
public sealed class FsPermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsPermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FsPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsPermissionsQuery(), cancellationToken));

    /// <summary>عملیات مجاز کاربر جاری روی واحد هدر (بیت‌های FsOperation).</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(FsMyOperationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        => Ok(new FsMyOperationsResponse(await _mediator.Send(new GetMyFsOperationsQuery(), cancellationToken)));

    [HttpPost]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] FsPermissionRequest request, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(
            new SaveFsPermissionCommand(null, request.UserId, request.UserName, request.UnitCode, request.IncludeSub, request.Operations), cancellationToken)));

    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] FsPermissionRequest request, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(
            new SaveFsPermissionCommand(id, request.UserId, request.UserName, request.UnitCode, request.IncludeSub, request.Operations), cancellationToken)));

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsPermissionCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }
}

public sealed record FsPermissionRequest(string UserId, string? UserName, string UnitCode, bool IncludeSub, FsOperation Operations);

public sealed record FsMyOperationsResponse(FsOperation Operations);
