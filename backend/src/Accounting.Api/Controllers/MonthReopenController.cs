using Accounting.Application.MonthReopen;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// برگشت صورتحساب ماه (DDL 073). صدور رمز فقط مدیر ستاد در واحد ستاد مرکزی (واحد مقصد در مسیر،
/// مثل <c>api/fs/periods/{unitCode}</c>)؛ اعمال رمز روی واحد هدر <c>X-Vahed-Code</c>. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/month-reopen")]
public sealed class MonthReopenController : ControllerBase
{
    private readonly IMediator _mediator;

    public MonthReopenController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>کاربر جاری می‌تواند رمز صادر کند / برگشت انجام دهد؟</summary>
    [HttpGet("access")]
    [ProducesResponseType(typeof(MonthReopenAccessDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccess(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMonthReopenAccessQuery(), cancellationToken));

    /// <summary>همهٔ واحدها برای انتخاب در فرم صدور رمز (فقط ستاد).</summary>
    [HttpGet("units")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthReopenUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnits(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMonthReopenUnitsQuery(), cancellationToken));

    /// <summary>سابقهٔ رمزهای یک سال — ستاد همهٔ واحدها، بقیه واحد هدر.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MonthReopenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLog([FromQuery] string year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMonthReopenLogQuery(year), cancellationToken));

    /// <summary><c>{ year, month, reason }</c> ⇒ رمز ۸ رقمی. رمز باز قبلی همان ماه دوباره برگردانده می‌شود.</summary>
    [HttpPost("{unitCode}/issue")]
    [ProducesResponseType(typeof(IssueMonthReopenCodeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(string unitCode, [FromBody] IssueMonthReopenCodeBody body, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new IssueMonthReopenCodeCommand(unitCode, body.Year, body.Month, body.Reason), cancellationToken));

    /// <summary><c>{ year, month, code }</c> ⇒ اسناد تأیید دائم آن ماه «بررسی‌شده» می‌شوند.</summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(ApplyMonthReopenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply([FromBody] ApplyMonthReopenBody body, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ApplyMonthReopenCommand(body.Year, body.Month, body.Code), cancellationToken));
}

public sealed record IssueMonthReopenCodeBody(string Year, int Month, string? Reason);

public sealed record ApplyMonthReopenBody(string Year, int Month, string Code);
