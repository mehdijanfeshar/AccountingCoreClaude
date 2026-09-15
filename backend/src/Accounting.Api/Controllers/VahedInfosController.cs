using Accounting.Application.Common;
using Accounting.Application.VahedInfos.Commands.CreateVahedInfo;
using Accounting.Application.VahedInfos.Commands.UpdateVahedInfo;
using Accounting.Application.VahedInfos.Queries;
using Accounting.Application.VahedInfos.Queries.GetVahedInfoById;
using Accounting.Application.VahedInfos.Queries.GetVahedInfos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_VAHED_INFO</c> (Legacy organizational unit / branch) write
/// and read use cases. Every action does nothing but: build a request → send it through
/// MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live
/// in the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update is exposed as <c>POST {id}/update</c>, mirroring
/// <see cref="AccountCodesController"/>.
///
/// <b>There is no Delete action at all, and this is deliberate, not an unfinished feature.</b>
/// <c>TB_VAHED_INFO</c> has no <c>ISDELETED</c> column — locked in by reflection in
/// <c>Accounting.Application.Tests.VahedInfos.VahedInfoSchemaAssumptionsTests</c> — and this
/// project never issues physical deletes (see CLAUDE.md). Adding a delete path here would
/// require either a physical <c>DELETE</c> statement (breaking Legacy referential integrity via
/// <c>FK_VAHEDINFO_TYPE</c> plus the several inbound FKs from <c>TB_TAFSILI</c>/
/// <c>TB_WHITELIST</c>/<c>TB_WORKSHOP</c>/self-reference, and diverging from how the original
/// Legacy application behaves) or an Oracle schema change (adding an <c>ISDELETED</c> column) —
/// and this project never alters the Oracle schema. Neither option is safe, so no delete path
/// exists.
///
/// <b>⚠️ No audit trail whatsoever.</b> <c>TB_VAHED_INFO</c> has no <c>ADDUSERID</c>,
/// <c>CHANGEUSERID</c>, <c>CREATEDDATE</c> or <c>UPDATEDDATE</c> column, so — unlike every
/// other entity in this project — Create/Update handlers do not (and cannot) depend on
/// <c>ICurrentUser</c>, and there is no way to know who created or last modified a row, or
/// when. This is a recorded gap in this table's schema, not something invented or fixed here.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_VAHED_INFO</c>
/// carries a real UNIQUE constraint (<c>UK_VAHEDINFO</c> on <c>VAHEDCODE</c>), mapped centrally
/// by <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers the one mapped FK violation</b>: a <c>VahedTypeId</c> that does not
/// reference an existing row violates <c>FK_VAHEDINFO_TYPE</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name). ⚠️ <c>CityId</c> and <c>ParentId</c> have NO mapped FK constraint at all,
/// so a bad value for either is written silently — a recorded referential-integrity gap, not
/// covered by this 400.
/// </summary>
[ApiController]
[Route("api/vahed-infos")]
public sealed class VahedInfosController : ControllerBase
{
    private readonly IMediator _mediator;

    public VahedInfosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new organizational unit (<c>TB_VAHED_INFO</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateVahedInfoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVahedInfoCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateVahedInfoResponse(id));
    }

    /// <summary>
    /// Returns a page of organizational units.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VahedInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetVahedInfosQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single organizational unit by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VahedInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVahedInfoByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing organizational unit (<c>TB_VAHED_INFO</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateVahedInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVahedInfoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVahedInfoCommand(
            id,
            request.VahedCode,
            request.VahedName,
            request.CityId,
            request.VahedTypeId,
            request.ParentId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateVahedInfoResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="VahedInfosController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_VAHED_INFO.ID</c>.</param>
public sealed record CreateVahedInfoResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="VahedInfosController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_VAHED_INFO.ID</c> that was updated (from the route).</param>
public sealed record UpdateVahedInfoResponse(Guid Id);

/// <summary>
/// Request body for <see cref="VahedInfosController.Update"/>. Mirrors every field of
/// <see cref="UpdateVahedInfoCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateVahedInfoRequest(
    string VahedCode,
    string VahedName,
    Guid CityId,
    Guid VahedTypeId,
    Guid? ParentId);
