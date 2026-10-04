using Accounting.Application.FinancialStatements.Dashboard;
using Accounting.Application.FinancialStatements.Ratios;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، ح-۸ — نسبت‌های مالی و روند (<c>docs/fs-module.md</c> §۱۳). Vahed-scoped؛ مالکیت مثل
/// قواعد کنترل. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs")]
public sealed class FsRatiosController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsRatiosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ratios")]
    [ProducesResponseType(typeof(IReadOnlyList<FsRatioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] FsFramework? framework, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsRatiosQuery(framework), cancellationToken));

    /// <summary>ح-۹ — داشبورد: شاخص‌ها (چهار نسبت اول) از آخرین اجرای منتشرشده و «کارهای من».</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(FsDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard([FromQuery] FsFramework framework, [FromQuery] string year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsDashboardQuery(framework, year), cancellationToken));

    [HttpGet("ratios/trend")]
    [ProducesResponseType(typeof(IReadOnlyList<FsRatioTrendYearDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrend([FromQuery] FsFramework framework, [FromQuery] string toYear, [FromQuery] int years = 5, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetFsRatioTrendQuery(framework, toYear, years), cancellationToken));

    [HttpGet("runs/{id:guid}/ratios")]
    [ProducesResponseType(typeof(IReadOnlyList<FsRatioValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunRatios(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsRunRatiosQuery(id), cancellationToken));

    [HttpPost("ratios")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateFsRatioCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, new FsIdResponse(await _mediator.Send(command, cancellationToken)));

    [HttpPost("ratios/{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFsRatioRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateFsRatioCommand(id, request.TitleFa, request.NumeratorExpr, request.DenominatorExpr, request.Format, request.OrderNo, request.IsActive),
            cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpPost("ratios/{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsRatioCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpPost("ratios/seed-defaults")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedDefaults(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SeedDefaultFsRatiosCommand(), cancellationToken));
}

public sealed record UpdateFsRatioRequest(string TitleFa, string NumeratorExpr, string? DenominatorExpr, FsRatioFormat Format, int OrderNo, bool IsActive);
