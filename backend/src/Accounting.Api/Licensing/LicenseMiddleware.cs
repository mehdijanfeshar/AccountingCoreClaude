using Accounting.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Licensing;

/// <summary>
/// اجرای لایسنس. در مهلت ارفاقی فقط هدر <c>X-License-Warning</c> می‌گذارد؛ پس از آن (یا بدون
/// لایسنس معتبر، وقتی <c>License:Enforce=true</c>) هر درخواستِ نوشتن (POST/…) را با ۴۰۳ رد می‌کند.
/// خواندن (GET) و POSTهای فقط‌خواندنی (انتهای مسیر مثل <c>/preview</c>) آزادند تا مشتری به
/// داده‌اش دسترسی داشته باشد.
/// </summary>
public sealed class LicenseMiddleware
{
    // POSTهایی که چیزی نمی‌نویسند (پیش‌نمایش/چاپ/جست‌وجو).
    private static readonly string[] ReadOnlyPostSuffixes = ["/preview", "/print", "/search", "/export"];

    private readonly RequestDelegate _next;

    public LicenseMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, LicenseService license, ICurrentUser currentUser)
    {
        // واحدی که کاربر در آن کار می‌کند؛ درستی هدر را خود IUnitScopeResolver جداگانه (۴۰۳) می‌سنجد.
        var unit = currentUser.IsAuthenticated
            ? (string.IsNullOrWhiteSpace(currentUser.RequestedVahedCode) ? currentUser.VahedCode : currentUser.RequestedVahedCode)
            : null;
        var status = license.GetStatus(unit);

        if (license.Enforce && status.State == LicenseState.Grace)
        {
            context.Response.Headers["X-License-Warning"] = $"expired;graceDaysLeft={status.GraceDaysLeft}";
        }

        if (license.Enforce && status.State == LicenseState.ReadOnly && IsWrite(context.Request))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = status.Problem is { } p && p.Contains("واحد") ? "برای این واحد لایسنس صادر نشده است؛ سامانه فقط‌خواندنی است. با پشتیبانی تماس بگیرید." : "لایسنس سامانه منقضی شده است؛ سامانه فقط‌خواندنی است. برای تمدید با پشتیبانی تماس بگیرید.",
                Instance = context.Request.Path,
            };
            problem.Extensions["code"] = "license_readonly";
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
            return;
        }

        await _next(context);
    }

    private static bool IsWrite(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? "";
        if (path.StartsWith("/api/license", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !ReadOnlyPostSuffixes.Any(s => path.EndsWith(s, StringComparison.OrdinalIgnoreCase));
    }
}
