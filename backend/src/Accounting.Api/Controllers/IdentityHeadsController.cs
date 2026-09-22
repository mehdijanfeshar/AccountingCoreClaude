using Accounting.Application.Common;
using Accounting.Application.IdentityHeads.Commands.Common;
using Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;
using Accounting.Application.IdentityHeads.Commands.DeleteIdentityHead;
using Accounting.Application.IdentityHeads.Commands.UpdateIdentityHead;
using Accounting.Application.IdentityHeads.Queries;
using Accounting.Application.IdentityHeads.Queries.GetIdentityHeadById;
using Accounting.Application.IdentityHeads.Queries.GetIdentityHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over شناسنامه (<c>TB_IDENTITYHEAD</c>) and its fixed values
/// (<c>TB_IDENTITYFIXITEMS</c>). Every action does nothing but build a request, send it through
/// MediatR, and map the result — all validation lives in FluentValidation validators and all
/// business rules in the Application layer.
///
/// <para>
/// <b>شناسنامه is not the same thing as «حساب‌های شناسه‌دار».</b> The table names invite exactly
/// that confusion: <c>TB_IDENTITY*</c> is شناسنامه, while حساب‌های شناسه‌دار is backed by
/// <c>TB_ATTRIBFORACCOUNTCODE</c> and lives in <see cref="AttribForAccountCodesController"/>. Full
/// write-up in <c>docs/centralaccount-business-reference.md</c> §۲۵.
/// </para>
///
/// <para>
/// <b>Fixed values are part of this aggregate; variable values are not.</b> A شناسنامه groups a
/// set of subgroups, each declared <c>Fixed</c> or <c>Variable</c>. The Fixed ones get one value
/// each, recorded here and travelling with the head on create/update/delete. The Variable ones
/// get a value per voucher line in <c>TB_IDENTITYDETAIL</c> — that table is deliberately untouched
/// by this controller, has no endpoint of its own anywhere, and belongs to voucher entry.
/// </para>
///
/// <para>
/// Every action implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy in <c>Program.cs</c>.
/// </para>
///
/// <para>
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate.</b>
/// Update/Delete are exposed as <c>POST</c> to <c>{id}/update</c> and <c>{id}/delete</c>.
/// </para>
///
/// <para>
/// <b>409 Conflict is declared on <see cref="Create"/> and <see cref="Update"/></b>:
/// <c>AK_AK_IDENTYHEAD_IDENTYHE</c> is UNIQUE on <c>(IDENTITYGROUPS_ID, SERIAL, VAHEDCODE, YEAR)</c>
/// and <c>AK_AK_IDENTYFIXITEMS_IDENTYFI</c> is UNIQUE on
/// <c>(IDENTITYHEAD_ID, IDENTITYSUBGRPS_ID, VAHEDCODE, YEAR)</c>; both are mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c>. The first is also how the deliberately non-atomic serial
/// assignment stays safe — see <c>IIdentityHeadRepository.GetNextSerialAsync</c>.
/// </para>
///
/// <para>
/// <b>400 also covers a mapped FK violation</b>: an unknown <c>IdentityGroupId</c> violates
/// <c>FK_IDENTYHE_IDENTYGR</c>, and an unknown <c>IdentitySubGroupId</c> violates
/// <c>FK_IDENTYFI_IDENTYSU</c>; both map centrally to 400.
/// </para>
///
/// ⚠️ <b>What is NOT checked:</b> that each supplied subgroup actually belongs to the chosen group,
/// and that it is a Fixed rather than a Variable one. The FKs only prove the rows exist. Legacy
/// enforces neither and no such rule was invented here; the UI cannot produce a mismatch because
/// it builds its inputs from <c>GET /api/identity-sub-groups?identityGroupId=…&amp;kind=1</c>, but a
/// hand-crafted request can. Recorded in <c>docs/open-decisions.md</c>.
/// </summary>
[ApiController]
[Route("api/identity-heads")]
public sealed class IdentityHeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityHeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates one شناسنامه together with its fixed values, in a single transaction. The serial is
    /// assigned server-side.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateIdentityHeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIdentityHeadCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateIdentityHeadResponse(id));
    }

    /// <summary>
    /// Returns a page of شناسنامه records with their fixed values.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="identityGroupId">Optional: only records of this group.</param>
    /// <param name="year">Optional: only records of this fiscal year.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <remarks>
    /// The organizational unit is deliberately not a parameter — every query is scoped to the
    /// caller's own unit from the token (risk #1).
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IdentityHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? identityGroupId = null,
        [FromQuery] string? year = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetIdentityHeadsQuery(pageNumber, pageSize, identityGroupId, year),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single شناسنامه by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IdentityHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIdentityHeadByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Replaces the fixed values of an existing شناسنامه. Exposed as <c>POST {id}/update</c>, not
    /// <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> comes from the route, never the
    /// body. Returns <b>200</b> with the affected <c>Id</c>, mirroring every other write action.
    ///
    /// The group and the serial are not editable — see <see cref="UpdateIdentityHeadCommand"/>.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(IdentityHeadWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateIdentityHeadRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateIdentityHeadCommand(id, request.FixItems), cancellationToken);

        return Ok(new IdentityHeadWriteResponse(id));
    }

    /// <summary>
    /// Soft-deletes one شناسنامه and its fixed values. Idempotent. Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(IdentityHeadWriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteIdentityHeadCommand(id), cancellationToken);

        return Ok(new IdentityHeadWriteResponse(id));
    }
}

/// <summary>Response body for <see cref="IdentityHeadsController.Create"/>.</summary>
public sealed record CreateIdentityHeadResponse(Guid Id);

/// <summary>Response body for the update and delete actions.</summary>
public sealed record IdentityHeadWriteResponse(Guid Id);

/// <summary>
/// Request body for <see cref="IdentityHeadsController.Update"/>. Mirrors
/// <see cref="UpdateIdentityHeadCommand"/> except <c>Id</c> (bound from the route) and
/// <c>VahedCode</c> (server-assigned, so not part of this body at all — not even as an ignored
/// field).
/// </summary>
public sealed record UpdateIdentityHeadRequest(IReadOnlyList<IdentityHeadFixItemInput>? FixItems);
