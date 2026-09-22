using Accounting.Application.Years.Queries;
using Accounting.Application.Years.Queries.GetYears;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Read-only HTTP surface over <c>TB_YEAR</c> — the financial years offered by the
/// «تغییر سال مالی و واحد» dialog. Equivalent of the reference project's <c>TbYear/GetAll</c>.
///
/// <para>
/// <b>Read-only on purpose.</b> The reference project also exposes
/// <c>TbYear/CreateFinancialYear</c>, which opens a financial year — an operation with real
/// consequences for voucher numbering (<c>LAST_NUMBER</c>). That is a separate decision with its
/// own rules and is deliberately out of scope here; this controller exists so a user can
/// <i>choose</i> a year, not create one.
/// </para>
///
/// <para>
/// No <c>VAHEDCODE</c> scoping: <c>TB_YEAR</c> has no such column, so financial years are global
/// to the installation. This action still implicitly returns <b>401 Unauthorized</b> under the
/// API-wide fallback policy.
/// </para>
/// </summary>
[ApiController]
[Route("api/years")]
public sealed class YearsController : ControllerBase
{
    private readonly IMediator _mediator;

    public YearsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns every financial year, newest first. Bare JSON array, never paginated — this table
    /// holds one row per year.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<YearDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetYearsQuery(), cancellationToken);

        return Ok(result);
    }
}
