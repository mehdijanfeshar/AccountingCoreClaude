using Accounting.Application.Common;
using Accounting.Application.IdentityGroups.Commands.CreateIdentityGroup;
using Accounting.Application.IdentityGroups.Commands.DeleteIdentityGroup;
using Accounting.Application.IdentityGroups.Commands.UpdateIdentityGroup;
using Accounting.Application.IdentityGroups.Queries;
using Accounting.Application.IdentityGroups.Queries.GetIdentityGroupById;
using Accounting.Application.IdentityGroups.Queries.GetIdentityGroups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_IDENTITYGROUP</c> (Legacy "identity"/شناسنامه main-group
/// definition, mapped to Oracle table <c>TB_IDENTITYGROUPS</c>) write and read use cases. Every
/// action does nothing but: build a request → send it through MediatR → map the result to an
/// <see cref="IActionResult"/>. All validation lives in FluentValidation validators (run by
/// <c>ValidationBehavior</c>) and all business rules live in the Application/Domain layers —
/// never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="AccountCodesController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_IDENTITYGROUP</c>
/// carries a real UNIQUE constraint (<c>UK_IDENTITYGROUPCODE</c> on
/// <c>(IDENTITYGROUPS_CODE, VAHEDCODE)</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers FK violations</b>: a <c>TafsiliId</c> that does not reference an existing
/// row violates <c>FK_IDENTITY_TAFSILI</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
/// </summary>
[ApiController]
[Route("api/identity-groups")]
public sealed class IdentityGroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityGroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new identity main-group (<c>TB_IDENTITYGROUP</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateIdentityGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIdentityGroupCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateIdentityGroupResponse(id));
    }

    /// <summary>
    /// Returns a page of identity main-groups.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IdentityGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetIdentityGroupsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single identity main-group by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IdentityGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIdentityGroupByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing identity main-group (<c>TB_IDENTITYGROUP</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateIdentityGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateIdentityGroupRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateIdentityGroupCommand(
            id,
            request.IdentityGroupsDesc,
            request.IdentityGroupsCode,
            request.VahedCode,
            request.TafsiliId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateIdentityGroupResponse(id));
    }

    /// <summary>
    /// Soft-deletes an identity main-group (<c>TB_IDENTITYGROUP.ISDELETED = true</c>). Exposed
    /// as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteIdentityGroupCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteIdentityGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteIdentityGroupCommand(id), cancellationToken);

        return Ok(new DeleteIdentityGroupResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="IdentityGroupsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_IDENTITYGROUP.ID</c>.</param>
public sealed record CreateIdentityGroupResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="IdentityGroupsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYGROUP.ID</c> that was updated (from the route).</param>
public sealed record UpdateIdentityGroupResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="IdentityGroupsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYGROUP.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteIdentityGroupResponse(Guid Id);

/// <summary>
/// Request body for <see cref="IdentityGroupsController.Update"/>. Mirrors every field of
/// <see cref="UpdateIdentityGroupCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateIdentityGroupRequest(
    string IdentityGroupsDesc,
    string? IdentityGroupsCode,
    string VahedCode,
    Guid? TafsiliId);
