using Accounting.Application.Common;
using Accounting.Application.AttribForAccountCodes.Commands.CreateAttribForAccountCode;
using Accounting.Application.AttribForAccountCodes.Commands.DeleteAttribForAccountCode;
using Accounting.Application.AttribForAccountCodes.Commands.UpdateAttribForAccountCode;
using Accounting.Application.AttribForAccountCodes.Queries;
using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;
using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_ATTRIBFORACCOUNTCODE</c> (Legacy per-account-code
/// identification-digit attribute definition) write and read use cases. Every action does
/// nothing but: build a request → send it through MediatR → map the result to an
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
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_ATTRIBFORACCOUNTCODE</c>
/// carries a real UNIQUE constraint (<c>AK_AK_ATTRIBFORMAINCO_ATTRIBFO</c> on
/// <c>(ACCOUNTCODE_ID, VAHEDCODE, YEAR)</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409. No pre-check is
/// performed — the DB constraint is the single source of truth, which also handles race
/// conditions correctly.
///
/// <b>400 also covers FK violations</b>: an <c>AccountCodeId</c> that does not reference an
/// existing row violates <c>FK_ATTRIBFO_ACCOUNTCODE</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle constraint
/// name).
/// </summary>
[ApiController]
[Route("api/attrib-for-account-codes")]
public sealed class AttribForAccountCodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttribForAccountCodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new per-account-code identification-digit attribute definition
    /// (<c>TB_ATTRIBFORACCOUNTCODE</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAttribForAccountCodeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAttribForAccountCodeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateAttribForAccountCodeResponse(id));
    }

    /// <summary>
    /// Returns a page of per-account-code identification-digit attribute definitions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AttribForAccountCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAttribForAccountCodesQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single per-account-code identification-digit attribute definition by
    /// <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AttribForAccountCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAttribForAccountCodeByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing per-account-code identification-digit attribute definition
    /// (<c>TB_ATTRIBFORACCOUNTCODE</c> row). Exposed as <c>POST {id}/update</c>, not <c>PUT</c> —
    /// by explicit project-owner mandate. <c>Id</c> is taken from the route, never the body.
    /// Returns <b>200</b> with the affected <c>Id</c> in the body (not 204), mirroring every
    /// other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateAttribForAccountCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAttribForAccountCodeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAttribForAccountCodeCommand(
            id,
            request.AccountCodeId,
            request.AttribBoxNo,
            request.Flag,
            request.LenAtr,
            request.AttribSum,
            request.ControlId,
            request.VahedCode,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateAttribForAccountCodeResponse(id));
    }

    /// <summary>
    /// Soft-deletes a per-account-code identification-digit attribute definition
    /// (<c>TB_ATTRIBFORACCOUNTCODE.ISDELETED = true</c>). Exposed as <c>POST {id}/delete</c>,
    /// not <c>DELETE</c> — by explicit project-owner mandate. Idempotent: a row that is already
    /// soft-deleted still returns 200 — see <see cref="DeleteAttribForAccountCodeCommandHandler"/>
    /// XML doc. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteAttribForAccountCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteAttribForAccountCodeCommand(id), cancellationToken);

        return Ok(new DeleteAttribForAccountCodeResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="AttribForAccountCodesController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ATTRIBFORACCOUNTCODE.ID</c>.</param>
public sealed record CreateAttribForAccountCodeResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AttribForAccountCodesController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ATTRIBFORACCOUNTCODE.ID</c> that was updated (from the route).</param>
public sealed record UpdateAttribForAccountCodeResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AttribForAccountCodesController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ATTRIBFORACCOUNTCODE.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteAttribForAccountCodeResponse(Guid Id);

/// <summary>
/// Request body for <see cref="AttribForAccountCodesController.Update"/>. Mirrors every field of
/// <see cref="UpdateAttribForAccountCodeCommand"/> except <c>Id</c>, which is bound from the
/// route instead.
/// </summary>
public sealed record UpdateAttribForAccountCodeRequest(
    Guid AccountCodeId,
    bool AttribBoxNo,
    bool Flag,
    byte LenAtr,
    bool AttribSum,
    bool? ControlId,
    string VahedCode,
    string Year);
