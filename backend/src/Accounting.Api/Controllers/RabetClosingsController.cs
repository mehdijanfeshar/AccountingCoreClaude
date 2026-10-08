using Accounting.Application.RabetClosings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>رابط اختتامیه (TB_RABET_CLOSING). الگوی پروژه: بدون PUT/DELETE — حذف نرم با <c>POST {id}/delete</c>.</summary>
[ApiController]
[Route("api/rabet-closings")]
public sealed class RabetClosingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RabetClosingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? year, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRabetClosingsQuery(year), cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(CreateRabetClosingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRabetClosingsCommand command, CancellationToken cancellationToken)
        => Ok(new CreateRabetClosingsResponse(await mediator.Send(command, cancellationToken)));

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteRabetClosingCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateRabetClosingsResponse(int Created);
