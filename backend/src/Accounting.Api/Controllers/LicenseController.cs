using Accounting.Api.Licensing;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// وضعیت لایسنس برای بنر هشدار در UI، و بارگذاری فایل لایسنس تازه (تمدید) توسط مدیر ستاد.
/// </summary>
[ApiController]
[Route("api/license")]
public sealed class LicenseController : ControllerBase
{
    private const long MaxFileBytes = 64 * 1024;

    private readonly LicenseService _license;
    private readonly ICurrentUser _currentUser;

    public LicenseController(LicenseService license, ICurrentUser currentUser)
    {
        _license = license;
        _currentUser = currentUser;
    }

    /// <summary>بدون احراز هویت است تا صفحهٔ ورود هم بتواند آن را نشان دهد؛ فقط نام مشتری و تاریخ انقضا.</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status() => Ok(ToDto(_license.GetStatus(CurrentUnit())));

    private string? CurrentUnit() => !_currentUser.IsAuthenticated ? null
        : string.IsNullOrWhiteSpace(_currentUser.RequestedVahedCode) ? _currentUser.VahedCode : _currentUser.RequestedVahedCode;

    /// <summary>
    /// تمدید: فایل <c>.lic</c> امضاشده را جایگزین می‌کند. فقط مدیر ستاد؛ فایل با امضای نامعتبر ذخیره نمی‌شود،
    /// پس مشتری نمی‌تواند لایسنس جعلی بگذارد. در حالت فقط‌خواندنی هم کار می‌کند (وگرنه تمدید ممکن نبود).
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(AppRoles.SetadAdmin))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "بارگذاری لایسنس فقط برای مدیر ستاد مجاز است.",
                Instance = Request.Path,
            });
        }

        if (file is null || file.Length == 0 || file.Length > MaxFileBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "فایل لایسنس خالی یا بیش از حد بزرگ است.",
                Instance = Request.Path,
            });
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var text = await reader.ReadToEndAsync(cancellationToken);

        if (!_license.Install(text))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "فایل لایسنس معتبر نیست (امضا یا قالب نادرست).",
                Instance = Request.Path,
            });
        }

        return Ok(ToDto(_license.GetStatus(CurrentUnit())));
    }

    private object ToDto(LicenseStatus s) => new
    {
        enforced = _license.Enforce,
        perUnit = _license.IsPerUnit,
        units = (_currentUser.IsAuthenticated ? _license.GetUnitStatuses() : []).Select(u => new
        {
            code = u.Code,
            state = u.Status.State.ToString(),
            expiresAt = u.Status.ExpiresAt?.ToString("yyyy-MM-dd"),
            daysLeft = u.Status.DaysLeft,
            graceDaysLeft = u.Status.GraceDaysLeft,
        }),
        state = s.State.ToString(),
        // نام مشتری برای درخواست بدون احراز هویت فاش نمی‌شود (این endpoint عمداً عمومی است).
        customer = _currentUser.IsAuthenticated ? s.Customer : null,
        expiresAt = s.ExpiresAt?.ToString("yyyy-MM-dd"),
        daysLeft = s.DaysLeft,
        graceDaysLeft = s.GraceDaysLeft,
        message = s.Problem,
    };
}
