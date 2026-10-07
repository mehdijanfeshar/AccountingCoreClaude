using Accounting.Application.Vouchers.Inbox;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>«دریافت اسناد از سایر سیستم‌ها» — صندوق سرسند/ردیف موقت (فاز ۵۲).</summary>
[ApiController]
[Route("api/system-voucher-inbox")]
public sealed class SystemVoucherInboxController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemVoucherInboxController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SystemVoucherInboxItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? year, [FromQuery] bool includeReceived, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSystemVoucherInboxQuery(year, includeReceived), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SystemVoucherInboxDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSystemVoucherInboxDetailQuery(id), ct));

    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(ReceiveSystemVoucherResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new ReceiveSystemVoucherCommand(id), ct));
}
