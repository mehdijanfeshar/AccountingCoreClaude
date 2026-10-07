using Accounting.Application.MonthReopen;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// صورتحساب ماه (DDL 074): ستاد اسناد بررسی‌شدهٔ یک ماه را برای گروه واحد یا یک واحد تأیید دائم می‌کند؛
/// لاگ با دلیل برای ستاد (همه) و واحد (خودش). فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/month-close")]
public sealed class MonthCloseController : ControllerBase
{
    private readonly IMediator _mediator;

    public MonthCloseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary><c>{ year, month, unitCategory: 1=بیمه‌ای|2=درمانی|3=ستادی|null=همه, unitCode }</c>.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MonthCloseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close([FromBody] CloseMonthBody body, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new CloseMonthCommand(body.Year, body.Month, body.UnitCategory, body.UnitCode), cancellationToken));

    [HttpGet("log")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthCloseLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLog([FromQuery] string year, [FromQuery] int? month, [FromQuery] string? unitCode, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMonthCloseLogQuery(year, month, unitCode), cancellationToken));
}

public sealed record CloseMonthBody(string Year, int Month, UnitCategory? UnitCategory, string? UnitCode);
