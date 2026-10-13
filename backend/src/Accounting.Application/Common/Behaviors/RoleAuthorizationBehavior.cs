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

    /// <summary>
    /// ماژول یک درخواست از روی namespace: <c>Accounting.Application.Commands.&lt;Module&gt;.…</c> و <c>Accounting.Application.Queries.&lt;Module&gt;.…</c>
    /// (پوشه‌های ریشهٔ Commands/Queries رد می‌شوند) یا <c>Accounting.Application.&lt;Module&gt;.…</c>.
    /// </summary>
    public static string ModuleOf(Type type)
    {
        var parts = (type.Namespace ?? string.Empty).Split('.');
        if (parts.Length <= 2 || parts[0] != "Accounting" || parts[1] != "Application")
            return string.Empty;

        return parts[2] is "Commands" or "Queries" && parts.Length > 3 ? parts[3] : parts[2];
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var type = typeof(TRequest);
        var module = ModuleOf(type);
        if (module is "" or "UnitAccess")
            return await next(cancellationToken);

        var isQuery = type.Name.EndsWith("Query", StringComparison.Ordinal);

        // لاگ تغییرات: منوی «مشاهده لاگ تغییرات» در جدول دسترسی نقش‌ها؛ پیش از استثنای مدیر ستاد (او هم باید صریحاً داشته باشد).
        if (module == AppRoles.AuditModule)
        {
            var auditSnapshot = _accessStore is null ? null : await _accessStore.GetSnapshotAsync(cancellationToken);
            var can = auditSnapshot is not null
                ? auditSnapshot.CanViewAuditLog(_currentUser.AllRoles)
                : _currentUser.IsInRole(AppRoles.Audit);
            if (isQuery && can)
                return await next(cancellationToken);
            throw new RoleAccessDeniedException("دسترسی به «مشاهده لاگ تغییرات» برای نقش شما تعریف نشده است.");
        }

        if (_currentUser.IsInRole(AppRoles.SetadAdmin))
            return await next(cancellationToken);

        // ویرایش کدینگ تا سطح معین و پیوند معین↔گروه تفصیلی: فقط مدیر ستاد؛ جدول «دسترسی نقش‌ها» این را باز نمی‌کند.
        if (!isQuery && AppRoles.HeadquartersOnlyModules.Contains(module))
            throw new RoleAccessDeniedException("ویرایش کدینگ تا سطح معین و پیوند معین به گروه تفصیلی فقط برای مدیر ستاد مجاز است.");

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
                // منوی دارای تب برای این ماژول: سطح همان تب (ارث‌بر از منو) تعیین می‌کند، نه کل منو.
                if (MenuCatalog.WriteKeysForModule(module).Any(k => snapshot.LevelFor(roles, k) >= RoleMenuAccessLevels.Edit))
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
