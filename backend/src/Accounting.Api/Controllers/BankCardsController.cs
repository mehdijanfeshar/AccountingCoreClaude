using Accounting.Application.BankCards;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// کارت حساب جاری (عملیات) — معادل «اسناد حساب جاری» سیستم قدیم روی <c>TB_BANKCARTDETAIL</c>:
/// ردیف دستی، دیسکت بانک رفاه، مغایرت‌گیری با چک/فیش و صورت مغایرت. فقط GET/POST؛ واحد از هدر.
/// <c>api/bank-cart-details</c> (CRUD خام) جدا می‌ماند.
/// </summary>
[ApiController]
[Route("api/bank-cards")]
public sealed class BankCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BankCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(BankCardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid bankAccountId, [FromQuery] string year = "", [FromQuery] string month = "",
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetBankCardQuery(year, bankAccountId, month), cancellationToken));

    [HttpGet("reconciliation")]
    [ProducesResponseType(typeof(BankCardReconciliationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReconciliation(
        [FromQuery] Guid bankAccountId, [FromQuery] string year = "", [FromQuery] string month = "",
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetBankCardReconciliationQuery(year, bankAccountId, month), cancellationToken));

    [HttpPost("rows")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRow([FromBody] SaveBankCardRowCommand command, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(command with { Id = null }, cancellationToken));

    [HttpPost("rows/{id:guid}/update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRow(Guid id, [FromBody] SaveBankCardRowCommand command, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpPost("rows/{id:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRow(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteBankCardRowCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("rows/{id:guid}/unreconcile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnreconcileRow(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new UnreconcileBankCardRowCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>دیسکت بانک رفاه (<c>STM001</c>) برای یک حساب و یک ماه.</summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BankCardImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Import([FromForm] BankCardImportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "فایل دیسکت الزامی است.");
            return ValidationProblem(ModelState);
        }

        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);
        return Ok(await _mediator.Send(
            new ImportBankCardDiskCommand(request.Year, request.BankAccountId, request.Month, request.File.FileName, stream.ToArray()),
            cancellationToken));
    }

    [HttpPost("reconcile")]
    [ProducesResponseType(typeof(BankCardReconcileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reconcile([FromBody] ReconcileBankCardCommand command, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(command, cancellationToken));
}

public sealed class BankCardImportRequest
{
    public Guid BankAccountId { get; set; }

    public string Year { get; set; } = string.Empty;

    public string Month { get; set; } = string.Empty;

    public IFormFile File { get; set; } = null!;
}
