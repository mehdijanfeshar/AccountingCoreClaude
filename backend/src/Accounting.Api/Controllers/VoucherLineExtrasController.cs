using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Application.Vouchers.Queries.LineExtras;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// اطلاعات تکمیلی ردیف سند (شناسهٔ حساب شناسه‌دار، ویژگی/شناسنامه، فیش بانک) — فقط خواندن؛ نوشتن همراه
/// خود ردیف (<c>extras</c> در ایجاد/ویرایش ردیف و در حسابیار). واحد فقط از هدر.
/// </summary>
[ApiController]
[Route("api/voucher-line-extras")]
public sealed class VoucherLineExtrasController : ControllerBase
{
    private readonly IMediator _mediator;
    public VoucherLineExtrasController(IMediator mediator) => _mediator = mediator;

    /// <summary>ردیفی با این معین و این تفصیلی‌ها چه چیزی لازم دارد.</summary>
    [HttpGet("requirements")]
    [ProducesResponseType(typeof(VoucherLineRequirements), StatusCodes.Status200OK)]
    public async Task<IActionResult> Requirements(
        [FromQuery] Guid accountId, [FromQuery] Guid[]? tafsiliIds, [FromQuery] string year, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetVoucherLineRequirementsQuery(accountId, tafsiliIds ?? [], year), ct));

    /// <summary>شناسنامه‌های یک ویژگی (با مقادیر ثابت).</summary>
    [HttpGet("identity-groups/{groupId:guid}/heads")]
    [ProducesResponseType(typeof(IReadOnlyList<IdentityHeadOption>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Heads(Guid groupId, [FromQuery] string year, CancellationToken ct) =>
        Ok(await _mediator.Send(new ListIdentityHeadOptionsQuery(groupId, year), ct));

    /// <summary>مقادیر ذخیره‌شدهٔ یک ردیف.</summary>
    [HttpGet("details/{detailId:guid}")]
    [ProducesResponseType(typeof(VoucherLineExtrasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Saved(Guid detailId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetVoucherLineExtrasQuery(detailId), ct));
}
