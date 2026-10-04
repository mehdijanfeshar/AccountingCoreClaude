using Accounting.Application.FinancialStatements.Consolidation;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، ط-۳ تا ط-۸ — تنظیمات مجموعه (حساب‌های نقد، تعدیلات سنواتی، XBRL)، قواعد حذف فی‌مابین،
/// شرکت‌های تابعه و تراز آن‌ها، نگاشت XBRL، کاربرگ و خروجی XBRL اجرا (<c>docs/fs-module.md</c> §۱۴). Vahed-scoped؛
/// فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/fs")]
public sealed class FsConsolidationController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsConsolidationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(IReadOnlyList<FsSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings([FromQuery] FsFramework? framework, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsSettingsQuery(framework), cancellationToken));

    /// <summary>کلیدها: CASH_SELECTOR، RESTATEMENT_SELECTOR، XBRL_SCHEMA_REF، XBRL_NAMESPACES، XBRL_ENTITY_SCHEME، XBRL_ENTITY_ID.</summary>
    [HttpPost("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SaveSetting([FromBody] SaveFsSettingCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("elim-rules")]
    [ProducesResponseType(typeof(IReadOnlyList<FsElimRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetElimRules([FromQuery] FsFramework? framework, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsElimRulesQuery(framework), cancellationToken));

    [HttpPost("elim-rules")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateElimRule([FromBody] FsElimRuleRequest r, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(
            new SaveFsElimRuleCommand(null, r.Framework, r.Shared, r.Code, r.TitleFa, r.LeftSelector, r.RightSelector, r.Tolerance, r.IsActive), cancellationToken)));

    [HttpPost("elim-rules/{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateElimRule(Guid id, [FromBody] FsElimRuleRequest r, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(
            new SaveFsElimRuleCommand(id, r.Framework, r.Shared, r.Code, r.TitleFa, r.LeftSelector, r.RightSelector, r.Tolerance, r.IsActive), cancellationToken)));

    [HttpPost("elim-rules/{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteElimRule(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsElimRuleCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpGet("entities")]
    [ProducesResponseType(typeof(IReadOnlyList<FsEntityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntities(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsEntitiesQuery(), cancellationToken));

    [HttpPost("entities")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEntity([FromBody] FsEntityRequest r, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(new SaveFsEntityCommand(null, r.Code, r.TitleFa, r.Currency, r.Ownership, r.IsActive), cancellationToken)));

    [HttpPost("entities/{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEntity(Guid id, [FromBody] FsEntityRequest r, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(new SaveFsEntityCommand(id, r.Code, r.TitleFa, r.Currency, r.Ownership, r.IsActive), cancellationToken)));

    [HttpPost("entities/{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntity(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsEntityCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpGet("entities/{id:guid}/tb")]
    [ProducesResponseType(typeof(FsEntityTbDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntityTb(Guid id, [FromQuery] string year, [FromQuery] int toMonth, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsEntityTbQuery(id, year, toMonth), cancellationToken));

    /// <summary>ورود تراز (جایگزینی کامل دوره) و نرخ‌ها. پاسخ = تعداد ردیف.</summary>
    [HttpPost("entities/{id:guid}/tb")]
    [ProducesResponseType(typeof(FsImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ImportEntityTb(Guid id, [FromBody] FsEntityTbRequest r, CancellationToken cancellationToken)
        => Ok(new FsImportResponse(await _mediator.Send(
            new ImportFsEntityTbCommand(id, r.Year, r.ToMonth, r.Rows, r.OpeningRate, r.ClosingRate, r.AverageRate), cancellationToken)));

    [HttpGet("xbrl-maps")]
    [ProducesResponseType(typeof(IReadOnlyList<FsXbrlMapDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetXbrlMaps(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsXbrlMapsQuery(), cancellationToken));

    [HttpPost("xbrl-maps")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateXbrlMap([FromBody] FsXbrlMapRequest r, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(new SaveFsXbrlMapCommand(null, r.TemplateCode, r.RowCode, r.Element, r.PeriodType), cancellationToken)));

    [HttpPost("xbrl-maps/{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateXbrlMap(Guid id, [FromBody] FsXbrlMapRequest r, CancellationToken cancellationToken)
        => Ok(new FsIdResponse(await _mediator.Send(new SaveFsXbrlMapCommand(id, r.TemplateCode, r.RowCode, r.Element, r.PeriodType), cancellationToken)));

    [HttpPost("xbrl-maps/{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteXbrlMap(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsXbrlMapCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    /// <summary>کاربرگ ترکیب/تلفیق: مبلغ ردیف‌های صورت‌های اصلی به تفکیک گروه، حذفیات و جمع.</summary>
    [HttpGet("runs/{id:guid}/worksheet")]
    [ProducesResponseType(typeof(FsWorksheetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorksheet(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsRunWorksheetQuery(id), cancellationToken));

    /// <summary>سند XBRL Instance اجرا (نگاشت و تنظیمات XBRL لازم است؛ نبودشان = ۴۰۹).</summary>
    [HttpGet("runs/{id:guid}/xbrl")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetXbrl(Guid id, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new GetFsRunXbrlQuery(id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }
}

public sealed record FsElimRuleRequest(
    FsFramework Framework, bool Shared, string Code, string TitleFa, string LeftSelector, string RightSelector, decimal Tolerance, bool IsActive);

public sealed record FsEntityRequest(string Code, string TitleFa, string Currency, decimal Ownership, bool IsActive);

public sealed record FsEntityTbRequest(
    string Year, int ToMonth, IReadOnlyList<FsEntityTbRowInput> Rows, decimal? OpeningRate, decimal? ClosingRate, decimal? AverageRate);

public sealed record FsXbrlMapRequest(string TemplateCode, string RowCode, string Element, int PeriodType);
