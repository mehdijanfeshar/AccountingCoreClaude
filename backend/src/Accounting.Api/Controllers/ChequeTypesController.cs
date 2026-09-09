using Accounting.Application.Common;
using Accounting.Application.ChequeTypes.Commands.CreateChequeType;
using Accounting.Application.ChequeTypes.Commands.DeleteChequeType;
using Accounting.Application.ChequeTypes.Commands.UpdateChequeType;
using Accounting.Application.ChequeTypes.Queries;
using Accounting.Application.ChequeTypes.Queries.GetChequeTypeById;
using Accounting.Application.ChequeTypes.Queries.GetChequeTypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_CHECK_TYPE</c> (Legacy cheque-print layout/configuration
/// template) write and read use cases. Every action does nothing but: build a request → send it
/// through MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live in
/// the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="AccountCodesController"/>.
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateChequeTypeCommand</c>/<c>UpdateChequeTypeCommand</c>/
/// <c>GetChequeTypesQuery</c> all implement <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c>,
/// so <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return 403 for this reason.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — like <see cref="PreDescribsController"/>/<see cref="WhiteListsController"/>,
/// <c>TB_CHECK_TYPE</c> carries no UNIQUE constraint at all, so declaring 409 would be
/// speculation about a constraint that does not exist.
///
/// <b>No FK exists on this table's own columns either</b>, so 400 here only ever comes from
/// FluentValidation failures — never from the central FK→400 mapping.
/// </summary>
[ApiController]
[Route("api/cheque-types")]
public sealed class ChequeTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChequeTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new cheque-print layout template (<c>TB_CHECK_TYPE</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateChequeTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateChequeTypeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateChequeTypeResponse(id));
    }

    /// <summary>
    /// Returns a page of cheque-print layout templates.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ChequeTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetChequeTypesQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single cheque-print layout template by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChequeTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetChequeTypeByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing cheque-print layout template (<c>TB_CHECK_TYPE</c> row).
    /// Exposed as <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate.
    /// <c>Id</c> is taken from the route, never the body. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateChequeTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateChequeTypeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateChequeTypeCommand(
            id,
            request.ChequeTypeTitle,
            request.ChequeWidth,
            request.ChequeHeight,
            request.ChequeImage,
            request.ChequeAdateFont,
            request.ChequeAdateLeft,
            request.ChequeAdateTop,
            request.ChequeAdateWidth,
            request.ChequeNdateFont,
            request.ChequeNdateLeft,
            request.ChequeNdateTop,
            request.ChequeNdateWidth,
            request.ChequeAamountFont,
            request.ChequeAamountLeft,
            request.ChequeAamountTop,
            request.ChequeAamountWidth,
            request.ChequeLamountFont,
            request.ChequeLamountLeft,
            request.ChequeLamountTop,
            request.ChequeLamountWidth,
            request.ChequeNamountFont,
            request.ChequeNamountLeft,
            request.ChequeNamountTop,
            request.ChequeNamountWidth,
            request.ChequeDescribe1Font,
            request.ChequeDescribe1Left,
            request.ChequeDescribe1Top,
            request.ChequeDescribe1Width,
            request.ChequeDescribe2Font,
            request.ChequeDescribe2Left,
            request.ChequeDescribe2Top,
            request.ChequeDescribe2Width,
            request.ChequeBreaklineFont,
            request.ChequeBreaklineLeft,
            request.ChequeBreaklineTop,
            request.ChequeBreaklineWidth,
            request.PrinterMargineTop,
            request.PrinterMargineLeft,
            request.PrinterType,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateChequeTypeResponse(id));
    }

    /// <summary>
    /// Soft-deletes a cheque-print layout template (<c>TB_CHECK_TYPE.ISDELETED = true</c>).
    /// Exposed as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteChequeTypeCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteChequeTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteChequeTypeCommand(id), cancellationToken);

        return Ok(new DeleteChequeTypeResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="ChequeTypesController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_CHECK_TYPE.ID</c>.</param>
public sealed record CreateChequeTypeResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ChequeTypesController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_CHECK_TYPE.ID</c> that was updated (from the route).</param>
public sealed record UpdateChequeTypeResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ChequeTypesController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_CHECK_TYPE.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteChequeTypeResponse(Guid Id);

/// <summary>
/// Request body for <see cref="ChequeTypesController.Update"/>. Mirrors every field of
/// <see cref="UpdateChequeTypeCommand"/> except <c>Id</c> (bound from the route instead) and
/// <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdateChequeTypeCommand.VahedCode"/> XML doc — so it is not part of this request
/// body at all, not even as an ignored field).
/// </summary>
public sealed record UpdateChequeTypeRequest(
    string? ChequeTypeTitle,
    byte? ChequeWidth,
    byte? ChequeHeight,
    byte[]? ChequeImage,
    string? ChequeAdateFont,
    byte? ChequeAdateLeft,
    byte? ChequeAdateTop,
    byte? ChequeAdateWidth,
    string? ChequeNdateFont,
    byte? ChequeNdateLeft,
    byte? ChequeNdateTop,
    byte? ChequeNdateWidth,
    string? ChequeAamountFont,
    byte? ChequeAamountLeft,
    byte? ChequeAamountTop,
    byte? ChequeAamountWidth,
    string? ChequeLamountFont,
    byte? ChequeLamountLeft,
    byte? ChequeLamountTop,
    byte? ChequeLamountWidth,
    string? ChequeNamountFont,
    byte? ChequeNamountLeft,
    byte? ChequeNamountTop,
    byte? ChequeNamountWidth,
    string? ChequeDescribe1Font,
    byte? ChequeDescribe1Left,
    byte? ChequeDescribe1Top,
    byte? ChequeDescribe1Width,
    string? ChequeDescribe2Font,
    byte? ChequeDescribe2Left,
    byte? ChequeDescribe2Top,
    byte? ChequeDescribe2Width,
    string? ChequeBreaklineFont,
    byte? ChequeBreaklineLeft,
    byte? ChequeBreaklineTop,
    byte? ChequeBreaklineWidth,
    byte? PrinterMargineTop,
    byte? PrinterMargineLeft,
    string? PrinterType,
    string Year);
