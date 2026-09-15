using Accounting.Application.SysTypes.Queries;
using Accounting.Application.SysTypes.Queries.GetSysTypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over <c>TB_SYSTYPE</c> (نوع سند) — the lookup behind the نوع سند filter on
/// the voucher list (<see cref="VoucherHeadsController.GetList"/>).
///
/// <b>Read-only.</b> This project never writes to this reference table, so there is no POST /
/// <c>{id}/update</c> / <c>{id}/delete</c> here — not an oversight, and not the PUT/DELETE
/// mandate either: the write use cases simply do not exist.
///
/// <b>No paging and no 403.</b> The table holds a handful of fixed rows shared by every unit and
/// has no <c>VAHEDCODE</c> column, so <c>GetSysTypesQuery</c> is not an <c>IVahedScopedQuery</c>
/// and <c>VahedScopeBehavior</c> never runs for it. 401 still applies via the API-wide fallback
/// policy (no <c>[AllowAnonymous]</c> here).
/// </summary>
[ApiController]
[Route("api/sys-types")]
public sealed class SysTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SysTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns every نوع سند, ordered by its Legacy code. Bare array, never paged.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SysTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSysTypesQuery(), cancellationToken);

        return Ok(result);
    }
}
