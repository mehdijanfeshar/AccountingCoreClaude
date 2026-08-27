using Accounting.Application.BillLogs.Commands.CreateBillLog;
using Accounting.Application.BillLogs.Commands.DeleteBillLog;
using Accounting.Application.BillLogs.Commands.UpdateBillLog;
using Accounting.Application.BillLogs.Queries;
using Accounting.Application.BillLogs.Queries.GetBillLogById;
using Accounting.Application.BillLogs.Queries.GetBillLogs;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_BILL_LOG</c> (Legacy bill/receipt log entry) write and read
/// use cases. Every action does nothing but: build a request → send it through MediatR → map
/// the result to an <see cref="IActionResult"/>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b> via the API-wide fallback
/// policy (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate</b>, exactly
/// as in <see cref="AccountCodesController"/>: update/delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — <c>TB_BILL_LOG</c> has no UNIQUE constraint and no FK at all, so no write to this table can
/// ever fail with a constraint violation from another row's data. 400 is still declared for
/// FluentValidation failures.
/// </summary>
[ApiController]
[Route("api/bill-logs")]
public sealed class BillLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new bill log entry (<c>TB_BILL_LOG</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBillLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBillLogCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateBillLogResponse(id));
    }

    /// <summary>
    /// Returns a page of bill log entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BillLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBillLogsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single bill log entry by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BillLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBillLogByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing bill log entry. Exposed as <c>POST {id}/update</c>, not
    /// <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is taken from the route, never
    /// the body. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateBillLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBillLogRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBillLogCommand(
            id,
            request.LogDesc,
            request.LogDate,
            request.VahedCode,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateBillLogResponse(id));
    }

    /// <summary>
    /// Soft-deletes a bill log entry (<c>ISDELETED = true</c>). Exposed as <c>POST {id}/delete</c>,
    /// not <c>DELETE</c> — by explicit project-owner mandate. Idempotent: a row that is already
    /// soft-deleted still returns 200. Returns <b>200</b> with the affected <c>Id</c> in the
    /// body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteBillLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBillLogCommand(id), cancellationToken);

        return Ok(new DeleteBillLogResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="BillLogsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_BILL_LOG.ID</c>.</param>
public sealed record CreateBillLogResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="BillLogsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_BILL_LOG.ID</c> that was updated (from the route).</param>
public sealed record UpdateBillLogResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="BillLogsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_BILL_LOG.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteBillLogResponse(Guid Id);

/// <summary>
/// Request body for <see cref="BillLogsController.Update"/>. Mirrors every field of
/// <see cref="UpdateBillLogCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateBillLogRequest(
    string? LogDesc,
    string? LogDate,
    string VahedCode,
    string Year);
