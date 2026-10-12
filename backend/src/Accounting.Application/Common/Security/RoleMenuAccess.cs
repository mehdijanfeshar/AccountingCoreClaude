namespace Accounting.Application.Common.Security;

/// <summary>
/// تصویر جدول دسترسی نقش‌ها به منوها (DDL 075). <see cref="IRoleMenuAccessStore.GetSnapshotAsync"/> وقتی جدول خالی است یا
/// هنوز ساخته نشده null برمی‌گرداند ⇒ رفتار ثابت قبلی (<see cref="AppRoles"/>).
/// </summary>
public sealed class RoleMenuAccessSnapshot
{
    private readonly Dictionary<string, Dictionary<string, int>> _byRole;

    public RoleMenuAccessSnapshot(IEnumerable<(string Role, string MenuKey, int Level)> rows)
    {
        _byRole = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (role, key, level) in rows)
        {
            if (!_byRole.TryGetValue(role, out var menus))
                _byRole[role] = menus = new Dictionary<string, int>(StringComparer.Ordinal);
            menus[key] = level;
        }
    }

    /// <summary>نقش‌هایی که در جدول ردیف دارند (حتی با سطح ۰).</summary>
    public IReadOnlyCollection<string> Roles => _byRole.Keys;

    public bool IsConfiguredRole(string role) => _byRole.ContainsKey(role);

    public IReadOnlyDictionary<string, int> ForRole(string role)
        => _byRole.TryGetValue(role, out var menus) ? menus : new Dictionary<string, int>();

    /// <summary>بیشترین سطح میان نقش‌های کاربر برای یک منو.</summary>
    public int LevelFor(IEnumerable<string> roles, string menuKey)
        => roles.Select(r => RoleLevel(r, menuKey))
            .DefaultIfEmpty(RoleMenuAccessLevels.None)
            .Max();

    /// <summary>
    /// سطح یک نقش برای یک کلید. تبِ (<c>menu#tab</c>) بی‌ردیف برای آن نقش، سطح منوی والد را ارث می‌برد — پس
    /// نقش‌هایی که پیش از افزودن تب‌ها پیکربندی شده‌اند رفتارشان عوض نمی‌شود.
    /// </summary>
    private int RoleLevel(string role, string key)
    {
        if (!_byRole.TryGetValue(role, out var m))
            return RoleMenuAccessLevels.None;
        if (m.TryGetValue(key, out var l))
            return l;
        return MenuCatalog.ParentMenuKey(key) is { } parent && m.TryGetValue(parent, out var pl) ? pl : RoleMenuAccessLevels.None;
    }

    /// <summary>
    /// دسترسی لاگ تغییرات: طبق جدول؛ تا وقتی هیچ نقشی برای این منو ردیف ندارد (پیش از اولین ذخیره) پیش‌فرض
    /// <see cref="AppRoles.Audit"/> است.
    /// </summary>
    public bool CanViewAuditLog(IEnumerable<string> roles)
        => _byRole.Values.Any(m => m.ContainsKey(MenuCatalog.AuditLogKey))
            ? LevelFor(roles, MenuCatalog.AuditLogKey) >= RoleMenuAccessLevels.View
            : roles.Contains(AppRoles.Audit, StringComparer.OrdinalIgnoreCase);

    /// <summary>سطح هر منو و هر تبِ فهرست برای کاربر (بیشترین میان نقش‌هایش).</summary>
    public Dictionary<string, int> MenuAccessFor(IReadOnlyCollection<string> roles)
        => MenuCatalog.All.Select(m => m.Key).Concat(MenuCatalog.Tabs.Select(t => t.Key))
            .ToDictionary(
                k => k,
                k => k == MenuCatalog.AuditLogKey
                    ? (CanViewAuditLog(roles) ? RoleMenuAccessLevels.View : RoleMenuAccessLevels.None)
                    : LevelFor(roles, k),
                StringComparer.Ordinal);
}

/// <summary>خواندن (با کش کوتاه) و نوشتن جدول دسترسی نقش‌ها.</summary>
public interface IRoleMenuAccessStore
{
    /// <summary>null = جدول خالی یا ساخته‌نشده (رفتار ثابت قبلی).</summary>
    Task<RoleMenuAccessSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>آیا جدول (DDL 075) وجود دارد.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>سطح هر منوی <paramref name="levels"/> برای نقش (upsert) و ذخیره؛ کش پاک می‌شود.</summary>
    Task SaveRoleAsync(string role, IReadOnlyDictionary<string, int> levels, string userId, CancellationToken cancellationToken = default);

    /// <summary>حذف همهٔ ردیف‌های نقش؛ کش پاک می‌شود.</summary>
    Task DeleteRoleAsync(string role, CancellationToken cancellationToken = default);
}
