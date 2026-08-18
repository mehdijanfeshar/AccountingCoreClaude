using Accounting.Application.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherHeadById;
using Accounting.Application.Vouchers.Queries.GetVoucherHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_VOUCHERSHEAD</c> (Legacy voucher header) write and read
/// use cases. Named "voucher heads" (not "vouchers") to match the underlying resource, so
/// voucher lines can later be exposed at <c>/api/voucher-heads/{id}/lines</c> without a rename.
/// Every action does nothing but: build a request → send it through MediatR → map the result
/// to an <see cref="IActionResult"/>.
/// </summary>
[ApiController]
[Route("api/voucher-heads")]
public sealed class VoucherHeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VoucherHeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new voucher head (<c>TB_VOUCHERSHEAD</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateVoucherHeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVoucherHeadCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateVoucherHeadResponse(id));
    }

    /// <summary>
    /// Returns a page of voucher heads, optionally filtered by <c>Year</c>/<c>VahedCode</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VoucherHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? year = null,
        [FromQuery] string? vahedCode = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetVoucherHeadsQuery(pageNumber, pageSize, year, vahedCode),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single voucher head by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VoucherHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVoucherHeadByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}

/// <summary>
/// Response body for a successful <see cref="VoucherHeadsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_VOUCHERSHEAD.ID</c>.</param>
public sealed record CreateVoucherHeadResponse(Guid Id);
