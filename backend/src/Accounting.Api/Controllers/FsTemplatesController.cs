using Accounting.Application.FinancialStatements.Commands.ActivateFsTemplateVersion;
using Accounting.Application.FinancialStatements.Commands.AddFsTemplateRow;
using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Application.FinancialStatements.Commands.CreateFsTemplate;
using Accounting.Application.FinancialStatements.Commands.CreateFsTemplateVersion;
using Accounting.Application.FinancialStatements.Commands.DeleteFsTemplate;
using Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateRow;
using Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateVersion;
using Accounting.Application.FinancialStatements.Commands.ImportFsTemplateRows;
using Accounting.Application.FinancialStatements.Commands.ReorderFsTemplateRows;
using Accounting.Application.FinancialStatements.Commands.SeedDefaultFsTemplates;
using Accounting.Application.FinancialStatements.Commands.UpdateFsTemplate;
using Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateRow;
using Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateVersion;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Application.FinancialStatements.Queries.AccountMapping;
using Accounting.Application.FinancialStatements.Queries.GetFsTemplates;
using Accounting.Application.FinancialStatements.Queries.GetFsTemplateVersion;
using Accounting.Application.FinancialStatements.Queries.ValidateFsTemplateVersion;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول «صورت‌های مالی»، بخش ۴۵-الف — قالب صورت، نسخه و ردیف (<c>docs/fs-module.md</c>). قالب‌ها
/// سراسری‌اند (هیچ action ای Vahed-scoped نیست و ۴۰۳ واحد نمی‌دهد). فقط GET/POST
/// (<c>{id}/update</c>، <c>{id}/delete</c>) — قاعدهٔ ۳ CLAUDE.md. هر action زیر سیاست پیش‌فرض
/// «کاربر احراز هویت‌شده» است (۴۰۱)؛ نقش/تفکیک وظایف ماژول در بخش ۴۵-د.
/// <list type="bullet">
/// <item>۴۰۰: اعتبارسنجی ورودی، نحو انتخاب‌گر/فرمول، و یافته‌های «خطا»ی فعال‌سازی (کلید = <c>RowCode.Field</c>).</item>
/// <item>۴۰۴: قالب/نسخه/ردیف ناموجود یا حذف‌شده.</item>
/// <item>۴۰۹: تغییر نسخهٔ غیرپیش‌نویس، پیش‌نویس دوم، کد تکراری، حذف قالب دارای نسخهٔ فعال.</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/fs")]
public sealed class FsTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FsTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// بخش ۴۵-و — «نگاشت حساب‌ها»: هر معین کدینگ به کدام ردیف قالب‌های این مجموعه رفته (همان قالب‌هایی که اجرا
    /// برای واحد هدر برمی‌دارد).
    /// </summary>
    [HttpGet("account-mapping")]
    [ProducesResponseType(typeof(IReadOnlyList<FsAccountMappingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAccountMapping(
        [FromQuery] FsFramework framework,
        [FromQuery] int year,
        [FromQuery] bool useDrafts = false,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetFsAccountMappingQuery(framework, year, useDrafts), cancellationToken));

    /// <summary>قالب‌ها با نسخه‌هایشان (جدیدترین نسخه اول).</summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<FsTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplates([FromQuery] FsFramework? framework, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetFsTemplatesQuery(framework), cancellationToken));

    /// <summary>قالب جدید + نسخهٔ پیش‌نویس ۱ (خالی).</summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(CreateFsTemplateResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateFsTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetVersion), new { versionId = result.VersionId }, result);
    }

    [HttpPost("templates/{id:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateFsTemplateRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateFsTemplateCommand(
                id, request.TitleFa, request.TitleEn, request.OrderNo,
                request.NoteParentTemplateCode, request.NoteParentRowCode, request.NoteTotalRowCode), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    [HttpPost("templates/{id:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsTemplateCommand(id), cancellationToken);
        return Ok(new FsIdResponse(id));
    }

    /// <summary>قالب‌های پیش‌فرض طرح بیمه‌ای و واحد تجاری (پیش‌نویس)؛ تکرارپذیر. پاسخ = کدهای ساخته‌شده.</summary>
    [HttpPost("templates/seed-defaults")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SeedDefaults(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SeedDefaultFsTemplatesCommand(), cancellationToken));

    /// <summary>نسخهٔ پیش‌نویس تازه با کپی ردیف‌های نسخهٔ مبدأ (پیش‌فرض: آخرین نسخه).</summary>
    [HttpPost("templates/{id:guid}/versions")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateFsTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        var versionId = await _mediator.Send(
            new CreateFsTemplateVersionCommand(id, request.SourceVersionId, request.Description), cancellationToken);
        return CreatedAtAction(nameof(GetVersion), new { versionId }, new FsIdResponse(versionId));
    }

    [HttpGet("template-versions/{versionId:guid}")]
    [ProducesResponseType(typeof(FsTemplateVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid versionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFsTemplateVersionQuery(versionId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("template-versions/{versionId:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateVersion(Guid versionId, [FromBody] UpdateFsTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateFsTemplateVersionCommand(versionId, request.Description), cancellationToken);
        return Ok(new FsIdResponse(versionId));
    }

    [HttpPost("template-versions/{versionId:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteVersion(Guid versionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsTemplateVersionCommand(versionId), cancellationToken);
        return Ok(new FsIdResponse(versionId));
    }

    /// <summary>اعتبارسنجی کامل نسخه بدون تغییر چیزی (خطا + هشدار).</summary>
    [HttpPost("template-versions/{versionId:guid}/validate")]
    [ProducesResponseType(typeof(FsTemplateCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateVersion(Guid versionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ValidateFsTemplateVersionQuery(versionId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>فعال‌سازی پیش‌نویس از سال مالی داده‌شده؛ هر یافتهٔ «خطا» = ۴۰۰.</summary>
    [HttpPost("template-versions/{versionId:guid}/activate")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateVersion(Guid versionId, [FromBody] ActivateFsTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateFsTemplateVersionCommand(versionId, request.EffectiveFromYear), cancellationToken);
        return Ok(new FsIdResponse(versionId));
    }

    [HttpPost("template-versions/{versionId:guid}/rows")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddRow(Guid versionId, [FromBody] FsTemplateRowInput row, CancellationToken cancellationToken)
    {
        var rowId = await _mediator.Send(new AddFsTemplateRowCommand(versionId, row), cancellationToken);
        return CreatedAtAction(nameof(GetVersion), new { versionId }, new FsIdResponse(rowId));
    }

    [HttpPost("template-versions/{versionId:guid}/rows/{rowId:guid}/update")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRow(Guid versionId, Guid rowId, [FromBody] FsTemplateRowInput row, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateFsTemplateRowCommand(versionId, rowId, row), cancellationToken);
        return Ok(new FsIdResponse(rowId));
    }

    [HttpPost("template-versions/{versionId:guid}/rows/{rowId:guid}/delete")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRow(Guid versionId, Guid rowId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFsTemplateRowCommand(versionId, rowId), cancellationToken);
        return Ok(new FsIdResponse(rowId));
    }

    /// <summary>ترتیب کامل ردیف‌ها (همهٔ شناسه‌ها به ترتیب جدید).</summary>
    [HttpPost("template-versions/{versionId:guid}/rows/reorder")]
    [ProducesResponseType(typeof(FsIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderRows(Guid versionId, [FromBody] ReorderFsTemplateRowsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ReorderFsTemplateRowsCommand(versionId, request.RowIds), cancellationToken);
        return Ok(new FsIdResponse(versionId));
    }

    /// <summary>جایگزینی کامل ردیف‌های پیش‌نویس (اتمیک). پاسخ = تعداد ردیف.</summary>
    [HttpPost("template-versions/{versionId:guid}/rows/import")]
    [ProducesResponseType(typeof(FsImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ImportRows(Guid versionId, [FromBody] ImportFsTemplateRowsRequest request, CancellationToken cancellationToken)
    {
        var count = await _mediator.Send(new ImportFsTemplateRowsCommand(versionId, request.Rows), cancellationToken);
        return Ok(new FsImportResponse(count));
    }
}

public sealed record FsIdResponse(Guid Id);

public sealed record FsImportResponse(int RowCount);

public sealed record UpdateFsTemplateRequest(
    string TitleFa,
    string? TitleEn,
    int OrderNo,
    string? NoteParentTemplateCode = null,
    string? NoteParentRowCode = null,
    string? NoteTotalRowCode = null);

public sealed record CreateFsTemplateVersionRequest(Guid? SourceVersionId, string? Description);

public sealed record UpdateFsTemplateVersionRequest(string? Description);

public sealed record ActivateFsTemplateVersionRequest(int EffectiveFromYear);

public sealed record ReorderFsTemplateRowsRequest(IReadOnlyList<Guid> RowIds);

public sealed record ImportFsTemplateRowsRequest(IReadOnlyList<FsTemplateRowInput> Rows);
