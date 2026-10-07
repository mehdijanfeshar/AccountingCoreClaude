using Accounting.Application.RoleAccess;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// دسترسی نقش‌ها به منوها (فاز ۵۴، DDL 075) — فقط مدیر ستاد. نام نقش در بدنه است نه مسیر، چون نقش‌ها فاصله دارند
/// («FINANCIAL CORE USER»). فقط GET/POST.
/// </summary>
[ApiController]
[Route("api/role-access")]
public sealed class RoleAccessController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleAccessController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>نقش‌ها، منوها و سطح دسترسی هر نقش به هر منو.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(RoleAccessDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetRoleAccessQuery(), cancellationToken));

    /// <summary>ذخیرهٔ سطح منوهای یک نقش (نقش تازه هم با همین ساخته می‌شود).</summary>
    [HttpPost("save")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save([FromBody] SaveRoleAccessCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>پر کردن جدول با رفتار فعلی برای نقش‌های ثابت.</summary>
    [HttpPost("seed-defaults")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SeedDefaults(CancellationToken cancellationToken)
    {
        await _mediator.Send(new SeedRoleAccessDefaultsCommand(), cancellationToken);
        return NoContent();
    }

    /// <summary>برداشتن همهٔ دسترسی‌های یک نقش از جدول.</summary>
    [HttpPost("remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove([FromBody] RemoveRoleAccessCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
