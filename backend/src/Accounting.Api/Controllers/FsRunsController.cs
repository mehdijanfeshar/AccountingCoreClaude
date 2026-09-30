using Accounting.Application.FinancialStatements.Commands.DeleteFsRun;
using Accounting.Application.FinancialStatements.Commands.GenerateFsRun;
using Accounting.Application.FinancialStatements.Commands.TransitionFsRun;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Application.FinancialStatements.Queries.Drill;
using Accounting.Application.FinancialStatements.Queries.GetFsRun;
using Accounting.Application.FinancialStatements.Queries.GetFsRuns;
using Accounting.Application.FinancialStatements.Queries.RunInsights;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، بخش ۴۵-ب — تهیهٔ صورت‌ها (اجرا) و خواندن Snapshot
/// (<c>docs/fs-module.md</c> §۷). همه Vahed-scoped: واحد فقط از هدر <c>X-Vahed-Code</c> (قاعدهٔ ۴)؛
/// اجرای واحد دیگر ۴۰۴ است. فقط GET/POST.
/// <list type="bullet">
/// <item>۴۰۰: ورودی نامعتبر.</item>
/// <item>۴۰۳: هدر واحد خارج از دسترس کاربر.</item>
/// <item>۴۰۹: مجموعه قالب قابل‌استفاده ندارد، یا قالب قابل محاسبه نیست (ارجاع/دور بین صورت‌ها).</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/fs/runs")]
public sealed class FsRunsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsRunsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FsRunSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList([FromQuery] string? year, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsRunsQuery(year), cancellationToken));

    /// <summary>تهیهٔ صورت‌ها (هم‌زمان). پاسخ ۲۰۱ با شناسهٔ اجرا.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate([FromBody] GenerateFsRunCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new FsIdResponse(id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FsRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsRunQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Drill-down سطح «حساب» (بخش ۴۵-د) — سهم معین‌ها در ردیف، از Snapshot.</summary>
    [HttpGet("{id:guid}/rows/{rowId:guid}/accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<FsDrillAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRowAccounts(Guid id, Guid rowId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsRunRowAccountsQuery(id, rowId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Drill-down سطح «واحد» — سهم زیرواحدهای سطح اول در ردیف یا در یک معین (<c>acc</c>).</summary>
    [HttpGet("{id:guid}/rows/{rowId:guid}/units")]
    [ProducesResponseType(typeof(IReadOnlyList<FsDrillUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRowUnits(Guid id, Guid rowId, [FromQuery] string? acc, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsRunRowUnitsQuery(id, rowId, string.IsNullOrWhiteSpace(acc) ? null : acc.Trim()), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Drill-down سطح «سند» — ردیف‌های سند معین <c>acc</c> (و زیرواحد <c>unit</c>)، زنده از اسناد، صفحه‌بندی‌شده.
    /// معین/واحدی که در ترکیب همین ردیف نیست = ۴۰۴.
    /// </summary>
    [HttpGet("{id:guid}/rows/{rowId:guid}/vouchers")]
    [ProducesResponseType(typeof(FsDrillVoucherPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRowVouchers(
        Guid id,
        Guid rowId,
        [FromQuery] string acc,
        [FromQuery] string? unit,
        [FromQuery] string column = "CUR",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetFsRunRowVouchersQuery(id, rowId, acc, string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(), column, page, pageSize),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>فایل Excel اجرا (بخش ۴۵-د): هر صورت/یادداشت یک برگه، جمع‌ها فرمول.</summary>
    [HttpGet("{id:guid}/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExcel(Guid id, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new GetFsRunExcelQuery(id), cancellationToken);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// گردش تأیید (بخش ۴۵-ه): <c>{ action: 1=Submit|2=Approve|3=Return|4=Publish, comments }</c>. پاسخ = وضعیت تازه.
    /// ۴۰۳ = تفکیک وظایف؛ ۴۰۹ = وضعیت نادرست، اجرای آزمایشی، یا کنترل مسدودکنندهٔ ناموفق.
    /// </summary>
    [HttpPost("{id:guid}/transitions")]
    [ProducesResponseType(typeof(FsRunTransitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transition(Guid id, [FromBody] FsRunTransitionRequest request, CancellationToken cancellationToken)
    {
        var state = await _mediator.Send(new TransitionFsRunCommand(id, request.Action, request.Comments), cancellationToken);
        return Ok(new FsRunTransitionResponse(id, state));
    }

    /// <summary>آیا اسناد پس از این اجرا تغییر کرده‌اند (بخش ۴۵-ه) — مانده‌ها را دوباره می‌خواند.</summary>
    [HttpGet("{id:guid}/staleness")]
    [ProducesResponseType(typeof(FsRunStalenessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaleness(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsRunStalenessQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>مقایسهٔ ردیف‌به‌ردیف دو اجرای همین واحد (بخش ۴۵-ه).</summary>
    [HttpGet("{id:guid}/diff/{otherId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<FsRunDiffRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiff(Guid id, Guid otherId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsRunDiffQuery(id, otherId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsRunCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }
}

public sealed record FsRunTransitionRequest(FsRunAction Action, string? Comments);

public sealed record FsRunTransitionResponse(Guid Id, FsRunState State);
