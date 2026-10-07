using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Common.Behaviors;

/// <summary>
/// اعمال نقش‌های سامانهٔ مالی روی همهٔ درخواست‌ها (معادل <c>[RolesAllowed]</c> سیستم قدیم روی Controllerها)
/// — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۵: «بلافاصله». اولین Behavior است. ماژول <c>UnitAccess</c> (کاربر جاری، واحدهای
/// مجاز) برای هر کاربر واردشده باز است تا UI بتواند نبود نقش را نشان دهد. نقش‌های اختصاصی ماژول‌ها (خزانه، تنخواه،
/// دسترسی صورت‌های مالی) جدا و علاوه بر این اعمال می‌شوند.
///
/// <para><b>دو حالت</b> (فاز ۵۴):</para>
/// <list type="bullet">
/// <item><b>ثابت</b> — تا جدول «دسترسی نقش‌ها» (DDL 075) خالی است: قاعدهٔ <see cref="AppRoles.AllowedFor"/>؛ خواندن (نام
/// درخواست با «Query» تمام شود) برای ۸ نقش، نوشتن بسته به ماژول.</item>
/// <item><b>پیکربندی‌شده</b> — نوشتن روی ماژولی که زیر منویی از <see cref="MenuCatalog"/> است، فقط اگر یکی از نقش‌های
/// کاربر در یکی از آن منوها «ثبت و تغییر» داشته باشد. خواندن برای هر کاربری که نقش ثابت یا نقش تعریف‌شده در جدول
/// دارد (فرم‌ها و فیلترها Queryهای ماژول‌های دیگر را هم می‌خوانند). ماژولی که زیر هیچ منویی نیست تابع قاعدهٔ ثابت است.</item>
/// </list>
/// مدیر ستاد در هر دو حالت همه‌کاره است تا کسی با جدول اشتباه خودش را بیرون نکند.
/// </summary>
public sealed class RoleAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser _currentUser;
    private readonly IRoleMenuAccessStore? _accessStore;

    public RoleAuthorizationBehavior(ICurrentUser currentUser, IRoleMenuAccessStore? accessStore = null)
    {
        _currentUser = currentUser;
        _accessStore = accessStore;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var type = typeof(TRequest);
        var parts = (type.Namespace ?? string.Empty).Split('.');
        var module = parts.Length > 2 && parts[0] == "Accounting" && parts[1] == "Application" ? parts[2] : string.Empty;
        if (module is "" or "UnitAccess")
            return await next(cancellationToken);

        var isQuery = type.Name.EndsWith("Query", StringComparison.Ordinal);

        if (_currentUser.IsInRole(AppRoles.SetadAdmin))
            return await next(cancellationToken);

        var snapshot = _accessStore is null ? null : await _accessStore.GetSnapshotAsync(cancellationToken);
        if (snapshot is not null)
        {
            var roles = _currentUser.AllRoles;
            var hasAnyRole = roles.Any(r => snapshot.IsConfiguredRole(r) || AppRoles.All.Contains(r));
            if (!hasAnyRole)
                throw new RoleAccessDeniedException(NoRoleMessage);

            var menus = MenuCatalog.MenusForModule(module);
            if (isQuery)
                return await next(cancellationToken);
            if (menus.Count > 0)
            {
                if (menus.Any(m => snapshot.LevelFor(roles, m.Key) >= RoleMenuAccessLevels.Edit))
                    return await next(cancellationToken);
                throw new RoleAccessDeniedException(
                    $"نقش شما برای «{menus[0].Title}» فقط اجازهٔ مشاهده دارد یا دسترسی ندارد؛ ثبت و تغییر مجاز نیست.");
            }
            // ماژول بیرون از فهرست منوها ⇒ قاعدهٔ ثابت پایین.
        }

        var allowed = AppRoles.AllowedFor(module, isQuery);
        if (allowed.Any(_currentUser.IsInRole))
            return await next(cancellationToken);

        var hasFixedRole = AppRoles.All.Any(_currentUser.IsInRole);
        throw new RoleAccessDeniedException(!hasFixedRole
            ? NoRoleMessage
            : isQuery
                ? "نقش شما اجازهٔ دیدن این بخش را ندارد."
                : AppRoles.ReferenceModules.Contains(module)
                    ? "تغییر اطلاعات پایه فقط با نقش «مدیر ستاد» ممکن است."
                    : "نقش شما فقط اجازهٔ مشاهده دارد؛ ثبت و تغییر برای این بخش مجاز نیست.");
    }

    private const string NoRoleMessage = "برای شما در سامانهٔ ورود نقشی در سامانهٔ مالی تعریف نشده است.";
}
