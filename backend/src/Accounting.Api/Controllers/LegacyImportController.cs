using Accounting.Api.Security;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.LegacyChart;
using Accounting.Application.LegacyImport;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// انتقال از سیستم قدیم (خط‌کدینگ، یک اسکیما برای هر واحد و هر سال). واحد: یافتن اسکیماها، دریافت کاربرگ کدینگ
/// (کد ملی/شناسهٔ ملی اشخاص را کاربر پر می‌کند) و بارگذاری آن؛ ستاد: کدینگ مشترک گروه/کل/معین، یک‌بار.
/// «بررسی» همه‌چیز را در تراکنش اجرا و برمی‌گرداند؛ «ثبت» (<c>commit=true</c>) ذخیره می‌کند.
/// </summary>
[ApiController]
[Route("api/legacy-import")]
public sealed class LegacyImportController : ControllerBase
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly IMediator _mediator;

    public LegacyImportController(IMediator mediator) => _mediator = mediator;

    /// <summary>اتصال دستی واحد جاری (سرور + اسکیمای هر سال)، بدون رمز.</summary>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(LegacyConnectionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetLegacyConnectionsQuery(), cancellationToken));

    /// <summary>
    /// همهٔ سال‌ها آزموده می‌شوند؛ <c>check=true</c> فقط آزمودن، وگرنه فقط اگر همه وصل شوند ذخیره می‌شود
    /// (<c>saved</c> در پاسخ). رمز خالی = رمز ذخیره‌شده بماند.
    /// </summary>
    [HttpPost("connections")]
    [ProducesResponseType(typeof(LegacyConnectionsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SaveConnections([FromBody] LegacyConnectionsInput input, [FromQuery] bool check, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new SaveLegacyConnectionsCommand(input, check), cancellationToken));

    /// <summary>رمزگشایی نام کاربر/رمز سرور انتخاب‌شده از فایل XML؛ رمز به مرورگر برنمی‌گردد (فقط توکن رمزگذاری‌شده).</summary>
    [HttpPost("xml-server")]
    [ProducesResponseType(typeof(LegacyXmlServerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReadXmlServer([FromBody] LegacyXmlServerInput input, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ReadLegacyXmlServerCommand(input), cancellationToken));

    /// <summary>اسکیماهای سالانهٔ واحد جاری: اتصال‌های دستی، یا اگر نباشد جستجو روی سرورهای تنظیمات.</summary>
    [HttpGet("sources")]
    [ProducesResponseType(typeof(IReadOnlyList<LegacySourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSources(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetLegacySourcesQuery(), cancellationToken));

    [HttpGet("status")]
    [ProducesResponseType(typeof(LegacyImportStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetLegacyImportStatusQuery(), cancellationToken));

    /// <summary>کاربرگ کدینگ واحد جاری؛ پس از ثبت، همان فایل پذیرفته‌شده با کدهای نهایی. <c>year</c> = فقط همان سال.</summary>
    [HttpGet("unit-workbook")]
    [Produces(Xlsx)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnitWorkbook([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new GetLegacyUnitWorkbookQuery(year), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>بارگذاری کاربرگ پرشدهٔ واحد؛ <c>commit=false</c> فقط بررسی.</summary>
    [HttpPost("unit-workbook")]
    [RequestSizeLimit(UploadGuard.DiskRequestLimit)]
    [ProducesResponseType(typeof(LegacyImportReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportUnitWorkbook(IFormFile file, [FromQuery] bool commit, CancellationToken cancellationToken)
    {
        var bytes = await ReadXlsxAsync(file, cancellationToken);
        if (bytes is null) return BadXlsx();
        return Ok(await _mediator.Send(new ImportLegacyUnitCodingCommand(bytes, commit), cancellationToken));
    }

    /// <summary>کاربرگ کدینگ مشترک (ستاد) از کل/معین واحدهای داده‌شده، مثلاً <c>?units=9516</c>.</summary>
    [HttpGet("chart-workbook")]
    [Produces(Xlsx)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChartWorkbook([FromQuery] string units, CancellationToken cancellationToken)
    {
        var codes = (units ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var file = await _mediator.Send(new GetLegacyChartWorkbookQuery(codes), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>بارگذاری کاربرگ کدینگ مشترک (فقط مدیر ستاد).</summary>
    [HttpPost("chart-workbook")]
    [RequestSizeLimit(UploadGuard.DiskRequestLimit)]
    [ProducesResponseType(typeof(LegacyImportReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportChartWorkbook(IFormFile file, [FromQuery] bool commit, CancellationToken cancellationToken)
    {
        var bytes = await ReadXlsxAsync(file, cancellationToken);
        if (bytes is null) return BadXlsx();
        return Ok(await _mediator.Send(new ImportLegacyChartCommand(bytes, commit), cancellationToken));
    }

    /// <summary>
    /// انتقال اسناد یک سال مالی؛ <c>commit=false</c> فقط بررسی. انتقال دوباره اسناد انتقال قبلی همان سال را حذف و
    /// از نو منتقل می‌کند.
    /// </summary>
    [HttpPost("vouchers")]
    [ProducesResponseType(typeof(LegacyImportReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportVouchers([FromQuery] int year, [FromQuery] bool commit, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ImportLegacyVouchersCommand(year, commit), cancellationToken));

    /// <summary>حساب‌های بانکی، نوع چک، دسته‌چک و برگ چک واحد از همهٔ سال‌ها؛ <c>commit=false</c> فقط بررسی. تکرارپذیر.</summary>
    [HttpPost("bank")]
    [ProducesResponseType(typeof(LegacyImportReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportBank([FromQuery] bool commit, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ImportLegacyBankCommand(commit), cancellationToken));

    /// <summary>سال‌هایی که اسنادشان منتقل شده (تعداد سند/ردیف و زمان).</summary>
    [HttpGet("vouchers")]
    [ProducesResponseType(typeof(IReadOnlyList<LegacyVoucherYearDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVoucherStatus(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetLegacyVoucherStatusQuery(), cancellationToken));

    /// <summary>فهرست شمارهٔ سند جدید ← قدیم یک سال منتقل‌شده.</summary>
    [HttpGet("vouchers/{year:int}/map")]
    [Produces(Xlsx)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVoucherMap(int year, CancellationToken cancellationToken)
    {
        var file = await _mediator.Send(new GetLegacyVoucherMapQuery(year), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<byte[]?> ReadXlsxAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0 || file.Length > UploadGuard.DiskFileMaxBytes
            || !Path.GetExtension(file.FileName ?? "").Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return null;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        // امضای zip (xlsx)
        return bytes.Length > 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04 ? bytes : null;
    }

    private IActionResult BadXlsx() => BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Bad Request",
        Detail = "فایل باید کاربرگ Excel (xlsx) دریافت‌شده از همین صفحه باشد.",
        Instance = Request.Path,
    });
}
