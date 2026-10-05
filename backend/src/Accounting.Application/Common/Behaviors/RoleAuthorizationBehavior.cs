using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Common.Behaviors;

/// <summary>
/// اعمال نقش‌های سامانهٔ مالی روی همهٔ درخواست‌ها (معادل <c>[RolesAllowed]</c> سیستم قدیم روی Controllerها)
/// — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۵: «بلافاصله». قاعده در <see cref="AppRoles.AllowedFor"/>: خواندن (نام
/// درخواست با «Query» تمام شود) برای همهٔ نقش‌ها؛ نوشتن بسته به ماژول. ماژول <c>UnitAccess</c> (کاربر جاری،
/// واحدهای مجاز) برای هر کاربر واردشده باز است تا UI بتواند نبود نقش را نشان دهد. اولین Behavior است.
/// نقش‌های اختصاصی ماژول‌ها (خزانه، تنخواه، دسترسی صورت‌های مالی) جدا و علاوه بر این اعمال می‌شوند.
/// </summary>
public sealed class RoleAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;

    public RoleAuthorizationBehavior(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var type = typeof(TRequest);
        var parts = (type.Namespace ?? string.Empty).Split('.');
        var module = parts.Length > 2 && parts[0] == "Accounting" && parts[1] == "Application" ? parts[2] : string.Empty;
        if (module is "" or "UnitAccess")
            return next(cancellationToken);

        var isQuery = type.Name.EndsWith("Query", StringComparison.Ordinal);
        var allowed = AppRoles.AllowedFor(module, isQuery);
        if (allowed.Any(_currentUser.IsInRole))
            return next(cancellationToken);

        var hasAnyRole = AppRoles.All.Any(_currentUser.IsInRole);
        throw new RoleAccessDeniedException(!hasAnyRole
            ? "برای شما در سامانهٔ ورود سازمان نقشی در سامانهٔ مالی تعریف نشده است."
            : isQuery
                ? "نقش شما اجازهٔ دیدن این بخش را ندارد."
                : AppRoles.ReferenceModules.Contains(module)
                    ? "تغییر اطلاعات پایه فقط با نقش «مدیر ستاد» ممکن است."
                    : "نقش شما فقط اجازهٔ مشاهده دارد؛ ثبت و تغییر برای این بخش مجاز نیست.");
    }
}
