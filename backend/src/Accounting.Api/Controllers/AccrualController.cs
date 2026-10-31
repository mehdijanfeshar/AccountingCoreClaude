using Accounting.Application.Accrual;
using Accounting.Domain.Accrual;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// حسابداری تعهدی، فاز ۶۶ (DDL 090؛ docs/accrual-phase66-task.md). ثبت سند تعهدی در حسابیار است؛ اینجا فقط
/// پیگیری، اجرای دستی و لغو زمان‌بندی. واحد فقط از هدر <c>X-Vahed-Code</c>. فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/accrual")]
public sealed class AccrualController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccrualController(IMediator mediator) => _mediator = mediator;

    /// <param name="state">۱ باز، ۲ اجراشده، ۳ لغو</param>
    /// <param name="kind">۱ برگشت برآورد، ۲ پیش‌پرداخت، ۳ درآمد انتقالی</param>
    /// <param name="from">سررسید از (شمسی، <c>14050801</c> یا <c>1405/08/01</c>)</param>
    /// <param name="to">سررسید تا</param>
    [HttpGet("schedules")]
    public async Task<ActionResult<IReadOnlyList<AccrualScheduleDto>>> List(
        [FromQuery] AccrualScheduleState? state, [FromQuery] AccrualScheduleKind? kind,
        [FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
        => Ok(await _mediator.Send(new ListAccrualSchedulesQuery(state, kind, from, to), ct));

    [HttpGet("schedules/by-voucher/{voucherId:guid}")]
    public async Task<ActionResult<AccrualVoucherScheduleDto>> ByVoucher(Guid voucherId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAccrualSchedulesByVoucherQuery(voucherId), ct));

    /// <summary>اجرا الان: سند برگشت یا سهم ماه با تاریخ سررسید، وضعیت «موقت». اجرای دوباره همان سند قبلی را برمی‌گرداند.</summary>
    [HttpPost("schedules/{id:guid}/run")]
    public async Task<ActionResult<AccrualRunResultDto>> Run(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new RunAccrualScheduleLineCommand { Id = id }, ct));

    /// <summary>لغو این سطر و سطرهای باز بعدیِ همان سند (فسخ قرارداد). دلیل الزامی.</summary>
    [HttpPost("schedules/{id:guid}/cancel")]
    public async Task<ActionResult<AccrualCancelResultDto>> Cancel(Guid id, [FromBody] CancelAccrualScheduleCommand cmd, CancellationToken ct)
    {
        cmd.Id = id;
        return Ok(await _mediator.Send(cmd, ct));
    }
}
