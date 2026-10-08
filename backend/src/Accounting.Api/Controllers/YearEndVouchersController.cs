using Accounting.Application.Vouchers.YearEnd;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// سند افتتاحیه و اختتامیه. واحد از هدر <c>X-Vahed-Code</c> (IVahedScoped)؛ سال در query/بدنه.
/// <c>kind</c>: <c>opening</c> (افتتاحیهٔ سال <c>year</c> از ماندهٔ سال قبل) یا <c>closing</c> (اختتامیهٔ سال <c>year</c>).
/// </summary>
[ApiController]
[Route("api/year-end-vouchers")]
public sealed class YearEndVouchersController(IMediator mediator) : ControllerBase
{
    [HttpGet("preview")]
    [ProducesResponseType(typeof(YearEndPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Preview([FromQuery] string kind, [FromQuery] string year, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetYearEndPreviewQuery(kind, year), cancellationToken));

    [HttpPost("issue")]
    [ProducesResponseType(typeof(YearEndIssueResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue([FromBody] IssueYearEndBody body, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new IssueYearEndVouchersCommand(body.Kind, body.Year), cancellationToken));
}

public sealed record IssueYearEndBody(string Kind, string Year);
