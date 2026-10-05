using System.Security.Claims;
using Accounting.Application.Common.Interfaces;

namespace Accounting.Api.Security;

/// <summary>
/// پس از احراز هویت، فقط اگر توکن claim «groups» نداشته باشد: نقش‌های کاربر را از پورتال سامانهٔ ورود (<see cref="IUserRoleProvider"/>) می‌خواند و
/// به‌صورت claim استاندارد role به کاربر جاری اضافه می‌کند، تا <c>IsInRole</c> در همه‌جا (از جمله
/// <c>RoleAuthorizationBehavior</c>) بی‌تغییر کار کند. توکن تأمین معمولاً نقش‌ها را در «groups» دارد و این مسیر فقط پشتیبان است.
/// </summary>
public sealed class PortalRoleClaimsMiddleware
{
    private readonly RequestDelegate _next;

    public PortalRoleClaimsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRoleProvider roleProvider)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true
            && user.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId
            // نقش‌ها معمولاً در خود توکن‌اند (claim «groups»)؛ پورتال فقط پشتیبان است.
            && !user.Claims.Any(c => c.Type.EndsWith("groups", StringComparison.OrdinalIgnoreCase)))
        {
            var roles = await roleProvider.GetRolesAsync(userId, context.RequestAborted);
            if (roles.Count > 0)
            {
                var identity = new ClaimsIdentity(
                    roles.Select(r => new Claim(ClaimTypes.Role, r)),
                    authenticationType: "idp-portal-roles",
                    nameType: ClaimTypes.Name,
                    roleType: ClaimTypes.Role);
                user.AddIdentity(identity);
            }
        }

        await _next(context);
    }
}
