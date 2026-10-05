namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// نقش‌های کاربر از سرویس پورتال سامانهٔ ورود سازمان — همان مسیر سیستم قدیم
/// (<c>CurrentUserRepository.GetCurrentUserAsync</c>: <c>GET {portal}/api/v2.0/users/{nationalCode}/info</c> →
/// <c>data.roles[].roleName</c>). توکن ورود کاربر نقش ندارد؛ نقش‌ها اینجا نگه داشته می‌شوند.
/// </summary>
public interface IUserRoleProvider
{
    /// <summary>نقش‌های کاربر؛ خطای سرویس ⇒ فهرست خالی (بستن پیش‌فرض، نه باز کردن).</summary>
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
}
