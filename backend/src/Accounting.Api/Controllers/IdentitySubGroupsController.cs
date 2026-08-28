using Accounting.Application.Common;
using Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;
using Accounting.Application.IdentitySubGroups.Commands.DeleteIdentitySubGroup;
using Accounting.Application.IdentitySubGroups.Commands.UpdateIdentitySubGroup;
using Accounting.Application.IdentitySubGroups.Queries;
using Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroupById;
using Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_IDENTITYSUBGRP</c> (Legacy "identity"/شناسنامه sub-group
/// definition, mapped to Oracle table <c>TB_IDENTITYSUBGRPS</c>) write and read use cases. Every
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
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_IDENTITYSUBGRP</c>
/// carries a real UNIQUE constraint (<c>AK_AK_IDENTYSUBGRPS_IDENTYSU</c> on
/// <c>(VAHEDCODE, YEAR, IDENTYSUBGROUPS_CODE)</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers FK violations</b>: an <c>IdentyGroupsId</c> that does not reference an
/// existing <c>TB_IDENTITYGROUP</c> row violates <c>FK_IDENTYSU_IDENTYGR</c> and is mapped
/// centrally to <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>,
/// no <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
/// </summary>
[ApiController]
[Route("api/identity-sub-groups")]
public sealed class IdentitySubGroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentitySubGroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new identity sub-group (<c>TB_IDENTITYSUBGRP</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateIdentitySubGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIdentitySubGroupCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateIdentitySubGroupResponse(id));
    }

    /// <summary>
    /// Returns a page of identity sub-groups.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IdentitySubGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetIdentitySubGroupsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single identity sub-group by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IdentitySubGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIdentitySubGroupByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing identity sub-group (<c>TB_IDENTITYSUBGRP</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateIdentitySubGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateIdentitySubGroupRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateIdentitySubGroupCommand(
            id,
            request.IdentyGroupsId,
            request.SubgrpsDesc,
            request.SubgrpsLen,
            request.SumFlag,
            request.Fixed,
            request.SubgrpsType,
            request.VahedCode,
            request.Year,
            request.IdentySubGroupsCode);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateIdentitySubGroupResponse(id));
    }

    /// <summary>
    /// Soft-deletes an identity sub-group (<c>TB_IDENTITYSUBGRP.ISDELETED = true</c>). Exposed
    /// as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteIdentitySubGroupCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteIdentitySubGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteIdentitySubGroupCommand(id), cancellationToken);

        return Ok(new DeleteIdentitySubGroupResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="IdentitySubGroupsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_IDENTITYSUBGRP.ID</c>.</param>
public sealed record CreateIdentitySubGroupResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="IdentitySubGroupsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYSUBGRP.ID</c> that was updated (from the route).</param>
public sealed record UpdateIdentitySubGroupResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="IdentitySubGroupsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_IDENTITYSUBGRP.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteIdentitySubGroupResponse(Guid Id);

/// <summary>
/// Request body for <see cref="IdentitySubGroupsController.Update"/>. Mirrors every field of
/// <see cref="UpdateIdentitySubGroupCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateIdentitySubGroupRequest(
    Guid IdentyGroupsId,
    string SubgrpsDesc,
    byte SubgrpsLen,
    bool SumFlag,
    bool Fixed,
    bool? SubgrpsType,
    string VahedCode,
    string Year,
    string? IdentySubGroupsCode);
