using Accounting.Application.UnitAccess.Queries;
using Accounting.Application.UnitAccess.Queries.GetAccessibleUnits;
using Accounting.Application.UnitAccess.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Everything about <b>the caller themselves</b> — identity, their own organizational unit, and
/// the units they may act as. Replaces the reference project's <c>CurrentUser/GetCurrentUser</c>
/// plus the unit-tree half of <c>VahedInfo</c>.
///
/// <para>
/// <b>Why <c>api/me</c> and not <c>api/users/{id}</c>.</b> Neither action takes a subject
/// parameter, and that is the security property, not a convenience: the subject is always the
/// bearer of the token. A route that accepted a user id or a unit code would immediately become
/// an enumeration surface ("show me what unit 0421 can reach"), which is precisely the class of
/// hole phases 19/32/33 closed.
/// </para>
///
/// <para>
/// Both actions implicitly return <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
/// </para>
///
/// <para>
/// ⚠️ <b>Read-only, and enforces nothing.</b> As of phase 37-A these endpoints only <i>describe</i>
/// what a caller could be allowed to do. The write path still resolves <c>VAHEDCODE</c> from the
/// token by exact equality (<c>VahedScopeBehavior</c>, <c>VahedOwnership</c>) and is unchanged, so
/// listing a unit here does not yet make its data reachable.
/// </para>
/// </summary>
[ApiController]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Describes the authenticated caller: user id, their own unit code and name, and whether
    /// that unit is headquarters. Returns <b>200</b> even when the token carries no usable org
    /// claim — in that case <c>vahedCode</c>/<c>vahedName</c> are null, which is a state the
    /// client needs to be able to see and report rather than a permissions failure.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Every organizational unit the caller may act as — their own unit plus its descendant
    /// subtree, or every unit when they are headquarters. Bare JSON array, never paginated: the
    /// «تغییر واحد» picker needs the whole set to build a tree, and organizational units are a
    /// small lookup rather than transactional data.
    ///
    /// Returns <b>403</b> when the token carries no usable org claim (<c>MissingVahedScopeException</c>)
    /// — deliberately different from <see cref="GetCurrentUser"/>, which reports that same state as
    /// data. Here there is no honest answer to give: an empty array would read as "you have access
    /// to nothing", which is indistinguishable from a correctly-configured user with no subtree.
    /// </summary>
    [HttpGet("accessible-units")]
    [ProducesResponseType(typeof(IReadOnlyList<AccessibleUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAccessibleUnits(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccessibleUnitsQuery(), cancellationToken);

        return Ok(result);
    }
}
