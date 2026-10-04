using Accounting.Application.FinancialStatements.Periods;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، ح-۵ — بستن دورهٔ صورت‌ها (<c>docs/fs-module.md</c> §۱۳). Vahed-scoped؛ واحد خارج از
/// زیردرخت هدر = ۴۰۳. فقط برای صورت‌ها (V-11 هنگام انتشار)؛ ثبت سند را نمی‌بندد. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs/periods")]
public sealed class FsPeriodsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsPeriodsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>واحد هدر و زیرواحدهای مستقیمش با وضعیت دوره و آخرین اجرای صورت‌ها.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(FsPeriodBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBoard([FromQuery] string year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsPeriodBoardQuery(year), cancellationToken));

    [HttpGet("{unitCode}/log")]
    [ProducesResponseType(typeof(IReadOnlyList<FsPeriodLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLog(string unitCode, [FromQuery] string year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsPeriodLogQuery(unitCode, year), cancellationToken));

    /// <summary>
    /// <c>{ year, action: 1=بستن|2=قفل|3=بازگشایی|4=درخواست بازگشایی|5=تأیید درخواست|6=رد درخواست, reason }</c>.
    /// پاسخ = وضعیت تازه. ۴۰۳ = واحد خارج از دسترس یا تأیید غیرستاد؛ ۴۰۹ = وضعیت نادرست.
    /// </summary>
    [HttpPost("{unitCode}/transitions")]
    [ProducesResponseType(typeof(FsPeriodTransitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transition(string unitCode, [FromBody] FsPeriodTransitionRequest request, CancellationToken cancellationToken)
    {
        var state = await _mediator.Send(new TransitionFsPeriodCommand(unitCode, request.Year, request.Action, request.Reason), cancellationToken);
        return Ok(new FsPeriodTransitionResponse(unitCode, request.Year, state));
    }
}

public sealed record FsPeriodTransitionRequest(string Year, FsPeriodAction Action, string? Reason);

public sealed record FsPeriodTransitionResponse(string UnitCode, string Year, FsPeriodState State);
