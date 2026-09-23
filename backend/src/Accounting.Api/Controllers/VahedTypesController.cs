using Accounting.Application.VahedTypes.Queries;
using Accounting.Application.VahedTypes.Queries.GetVahedTypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Read-only HTTP surface over <c>TB_VAHED_TYPE</c> — the organisational unit types that the
/// «دسترسی کدینگ حسابداری» screen grants account codes against, and that
/// <c>TB_WHITEANDBLACKLIST.VAHEDTYPE_ID</c> references.
///
/// <b>There is deliberately no write action of any kind here</b> — no POST, no update, no delete.
/// Unit types arrive with the shared <c>CENTRALACCOUNT</c> schema and are consumed by other
/// systems on it; this project never creates or reshapes them. The absence is asserted by
/// <c>NoWriteActionExistsOnVahedTypesControllerTests</c> so it stays deliberate rather than
/// becoming an oversight someone "fixes" later. Compare <see cref="VahedInfosController"/>, whose
/// missing Delete is documented the same way.
///
/// The single action implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
/// </summary>
[ApiController]
[Route("api/vahed-types")]
public sealed class VahedTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VahedTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns every unit type, ordered by <c>PARENTTYPECODE</c> then <c>TYPECODE</c>.
    ///
    /// Unpaged on purpose — see <see cref="GetVahedTypesQuery"/>: it is a fixed 17-row lookup
    /// whose only consumer is a checkbox tree, and a page of a tree is not a thing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VahedTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVahedTypesQuery(), cancellationToken);

        return Ok(result);
    }
}
