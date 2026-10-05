using Accounting.Application.ChequeBook;
using Accounting.Application.Common;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// دفتر چک (عملیات) — چک‌های به‌کاررفته در اسناد، کارتابل «دستور پرداخت ⇐ تاییدیه چک»، ابطال و چاپ چک.
/// فقط GET/POST؛ واحد از هدر.
/// </summary>
[ApiController]
[Route("api/cheque-book")]
public sealed class ChequeBookController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChequeBookController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ChequeBookItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string year = "",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? bankAccountId = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] bool? canceled = null,
        [FromQuery] bool? printed = null,
        [FromQuery] string? chequeNo = null,
        [FromQuery] decimal? amount = null,
        [FromQuery] string? description = null,
        [FromQuery] ChequeApprovalState? approvalState = null,
        [FromQuery] bool onlyUnissued = false,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetChequeBookQuery(year, pageNumber, pageSize, bankAccountId, fromDate, toDate, canceled, printed,
                chequeNo, amount, description, approvalState, onlyUnissued),
            cancellationToken));

    /// <summary>چک‌های قابل انتخاب برای ردیف سند.</summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableChequeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] Guid? accountCodeId = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetAvailableChequesQuery(accountCodeId, search), cancellationToken));

    /// <summary>دسته‌چک‌های صوری حساب‌های بانکی همین معین، با شمارهٔ بعدی (شماره هنگام ثبت سند صادر می‌شود).</summary>
    [HttpGet("sori-books")]
    [ProducesResponseType(typeof(IReadOnlyList<SoriChequeBookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSoriBooks([FromQuery] Guid? accountCodeId = null, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetSoriChequeBooksQuery(accountCodeId), cancellationToken));

    [HttpGet("{checkId:guid}")]
    [ProducesResponseType(typeof(ChequeSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOne(Guid checkId, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetChequeQuery(checkId), cancellationToken));

    [HttpGet("{checkId:guid}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<ChequeApprovalEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(Guid checkId, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetChequeApprovalEventsQuery(checkId), cancellationToken));

    [HttpGet("{checkId:guid}/print")]
    [ProducesResponseType(typeof(ChequePrintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetPrint(Guid checkId, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetChequePrintQuery(checkId), cancellationToken));

    [HttpPost("{checkId:guid}/printed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkPrinted(Guid checkId, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new MarkChequePrintedCommand(checkId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{checkId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid checkId, [FromQuery] bool canceled = true, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new SetChequeCanceledCommand(checkId, canceled), cancellationToken);
        return NoContent();
    }

    /// <summary>کارتابل: <c>action</c> 1 صدور دستور پرداخت، 2 تأیید (مرحلهٔ جاری)، 4 برگشت.</summary>
    [HttpPost("approval")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approval([FromBody] ChequeApprovalCommand command, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(command, cancellationToken));
}
