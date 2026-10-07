using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Security;

/// <summary>«کاربر ستاد مرکزی» = نقش مدیر ستاد و واحد خودِ کاربر ستاد مرکزی باشد (همان قاعدهٔ ReportUnitScope).</summary>
public static class HeadquartersAccess
{
    public static async Task<bool> IsHeadquartersAdminAsync(ICurrentUser user, IUnitAccessReadRepository unitAccess, CancellationToken ct)
    {
        if (!user.IsInRole(AppRoles.SetadAdmin) || user.VahedCode is not { Length: > 0 } own)
            return false;
        return (await unitAccess.GetUnitProfileAsync(own, ct))?.IsHeadquarters == true;
    }

    /// <summary>گروه واحد (بیمه‌ای/درمانی/ستادی) از نوع واحد؛ null اگر نوع واحد نامعلوم باشد.</summary>
    public static async Task<UnitCategory?> CategoryOfAsync(string vahedCode, IUnitAccessReadRepository unitAccess, CancellationToken ct)
    {
        var node = (await unitAccess.GetAllUnitsAsync(ct)).FirstOrDefault(u => u.VahedCode == vahedCode);
        return UnitCategories.Of(node?.TypeCode);
    }
}

/// <summary>
/// «کاربر ستاد مرکزی» و «دید سطح کشور» برای درخواست جاری (یک بار خوانده می‌شود). قاعدهٔ یکسان همهٔ جاهایی که
/// انتخاب گروه واحد (بیمه‌ای/درمانی/ستادی) یا تعریف سراسری دارند — تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷.
/// </summary>
public interface IHeadquartersAccessService
{
    /// <summary>نقش مدیر ستاد و واحد خود کاربر ستاد مرکزی — تعریف موارد سراسری.</summary>
    Task<bool> IsHeadquartersAdminAsync(CancellationToken ct = default);

    /// <summary>مدیر ستاد مرکزی یا نقش مدیریتی سطح کشور.</summary>
    Task<bool> CanSeeCountryAsync(CancellationToken ct = default);

    /// <summary>
    /// قابلیت زیرمنو (<see cref="AbilityCatalog"/>): مدیر ستاد مرکزی همیشه؛ وقتی «دسترسی نقش‌ها» پیکربندی شده، طبق جدول؛
    /// وگرنه قاعدهٔ پیش‌فرض قابلیت.
    /// </summary>
    Task<bool> HasAbilityAsync(string ability, CancellationToken ct = default);

    /// <summary>قابلیت‌هایی که کاربر جاری دارد (برای <c>/api/me</c>).</summary>
    Task<IReadOnlyList<string>> GrantedAbilitiesAsync(CancellationToken ct = default);
}

public sealed class HeadquartersAccessService : IHeadquartersAccessService
{
    private readonly ICurrentUser _user;
    private readonly IUnitAccessReadRepository _unitAccess;
    private readonly IRoleMenuAccessStore? _store;
    private bool? _isHqAdmin;

    public HeadquartersAccessService(ICurrentUser user, IUnitAccessReadRepository unitAccess, IRoleMenuAccessStore? store = null)
    {
        _user = user;
        _unitAccess = unitAccess;
        _store = store;
    }

    public async Task<bool> IsHeadquartersAdminAsync(CancellationToken ct = default)
        => _isHqAdmin ??= await HeadquartersAccess.IsHeadquartersAdminAsync(_user, _unitAccess, ct);

    public async Task<bool> CanSeeCountryAsync(CancellationToken ct = default)
        => _user.IsInRole(AppRoles.National) || await IsHeadquartersAdminAsync(ct);

    public async Task<bool> HasAbilityAsync(string ability, CancellationToken ct = default)
    {
        if (await IsHeadquartersAdminAsync(ct))
            return true;
        var definition = AbilityCatalog.All.FirstOrDefault(a => a.Key == ability)
            ?? throw new InvalidOperationException($"Unknown ability '{ability}'.");
        var snapshot = _store is null ? null : await _store.GetSnapshotAsync(ct);
        if (snapshot is not null)
            return snapshot.LevelFor(_user.AllRoles, AbilityCatalog.StorageKey(ability)) >= RoleMenuAccessLevels.View;
        return definition.Default == AbilityDefault.Country && await CanSeeCountryAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GrantedAbilitiesAsync(CancellationToken ct = default)
    {
        var granted = new List<string>();
        foreach (var a in AbilityCatalog.All)
            if (await HasAbilityAsync(a.Key, ct))
                granted.Add(a.Key);
        return granted;
    }
}
