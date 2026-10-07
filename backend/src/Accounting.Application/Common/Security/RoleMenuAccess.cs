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
        => roles.Select(r => _byRole.TryGetValue(r, out var m) && m.TryGetValue(menuKey, out var l) ? l : RoleMenuAccessLevels.None)
            .DefaultIfEmpty(RoleMenuAccessLevels.None)
            .Max();

    /// <summary>سطح هر منوی فهرست برای کاربر (بیشترین میان نقش‌هایش).</summary>
    public Dictionary<string, int> MenuAccessFor(IReadOnlyCollection<string> roles)
        => MenuCatalog.All.ToDictionary(m => m.Key, m => LevelFor(roles, m.Key), StringComparer.Ordinal);
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
