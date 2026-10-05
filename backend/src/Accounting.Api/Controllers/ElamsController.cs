using Accounting.Application.Common;
using Accounting.Application.Elams;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// اعلامیه (عملیات) — سایر اعلامیهٔ صادره/رسیده و اعلامیهٔ صادرهٔ درآمد، با صدور سند و گردش تأیید.
/// فقط GET/POST (<c>{id}/update</c>، <c>{id}/delete</c>)؛ واحد از هدر <c>X-Vahed-Code</c>.
/// <c>api/elam-heads</c> (CRUD خام سرسند) جدا و دست‌نخورده می‌ماند.
/// </summary>
[ApiController]
[Route("api/elams")]
public sealed class ElamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ElamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>کارتابل: <paramref name="kind"/> 1 صادره، 2 رسیده، 3 درآمد.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ElamCartableItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartable(
        [FromQuery] string year = "",
        [FromQuery] ElamKind kind = ElamKind.Sent,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? serialFrom = null,
        [FromQuery] string? serialTo = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? dabirNo = null,
        [FromQuery] string? counterVahedCode = null,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetElamCartableQuery(year, kind, pageNumber, pageSize, serialFrom, serialTo, dateFrom, dateTo, dabirNo, counterVahedCode),
            cancellationToken));

    [HttpGet("units")]
    [ProducesResponseType(typeof(IReadOnlyList<ElamUnitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnits(CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetElamUnitsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ElamViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetElamQuery(id), cancellationToken));

    [HttpPost("other")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOther([FromBody] CreateOtherElamCommand command, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("other/{id:guid}/update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOther(Guid id, [FromBody] UpdateOtherElamCommand command, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("revenue")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRevenue([FromBody] CreateRevenueElamCommand command, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(command, cancellationToken));

    [HttpPost("revenue/{id:guid}/update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRevenue(Guid id, [FromBody] UpdateRevenueElamCommand command, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteElamCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>صدور سند موقت.</summary>
    [HttpPost("{id:guid}/voucher")]
    [ProducesResponseType(typeof(ElamVoucherIssuedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IssueVoucher(Guid id, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new IssueElamVoucherCommand(id), cancellationToken));

    /// <summary>تأیید اولیه.</summary>
    [HttpPost("{id:guid}/confirm-first")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmFirst(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ConfirmElamCommand(id, false), cancellationToken);
        return NoContent();
    }

    /// <summary>تأیید نهایی و ارسال — برای صادره، شناسهٔ اعلامیهٔ رسیدهٔ ساخته‌شده در واحد مقصد.</summary>
    [HttpPost("{id:guid}/confirm-final")]
    [ProducesResponseType(typeof(Guid?), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmFinal(Guid id, CancellationToken cancellationToken = default)
        => Ok(new { receivedElamId = await _mediator.Send(new ConfirmElamCommand(id, true), cancellationToken) });
}
