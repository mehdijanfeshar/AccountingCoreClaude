using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Sales;

namespace Accounting.Application.Sales;

/// <param name="IsAdmin">مدیر ستاد یا مدیر مالی واحد — همهٔ نقش‌های فروش را دارد و نقش‌ها را تعیین می‌کند.</param>
public sealed record SalesAccessInfo(string UserId, IReadOnlySet<SalesRole> Roles, bool IsAdmin)
{
    public bool Has(SalesRole role) => IsAdmin || Roles.Contains(role);
    public bool Any => IsAdmin || Roles.Count > 0;
    /// <summary>تنظیمات، نگاشت حساب و فهرست کالا: مدیر فروش یا مدیر.</summary>
    public bool CanManage => IsAdmin || Roles.Contains(SalesRole.Manager);
}

/// <summary>
/// نقش فروش کاربر در واحد (<c>TB_SL_USER_ROLE</c>) — جدا از نقش‌های ثابت توکن، چون تفکیک وظایف فروش
/// (کارشناس / مدیر تأییدکننده / حسابدار قطعی‌کننده) در هر واحد بین کاربران همان نقش توکن است.
/// </summary>
public sealed class SalesAccess
{
    private readonly ISalesRepository _repo;
    private readonly ICurrentUser _user;

    public SalesAccess(ISalesRepository repo, ICurrentUser user) { _repo = repo; _user = user; }

    public async Task<SalesAccessInfo> GetAsync(string vahedCode, CancellationToken ct)
    {
        var roles = (await _repo.ListRolesAsync(vahedCode, _user.UserId, ct)).Select(r => r.Role).ToHashSet();
        var admin = _user.IsInRole(AppRoles.SetadAdmin) || _user.IsInRole(AppRoles.MaliAdmin);
        return new SalesAccessInfo(_user.UserId, roles, admin);
    }

    public async Task<SalesAccessInfo> RequireAsync(string vahedCode, SalesRole role, CancellationToken ct)
    {
        var a = await GetAsync(vahedCode, ct);
        if (!a.Has(role))
            throw new RoleAccessDeniedException($"این کار نقش فروش «{RoleTitle(role)}» در این واحد را لازم دارد.");
        return a;
    }

    public async Task<SalesAccessInfo> RequireAnyAsync(string vahedCode, CancellationToken ct)
    {
        var a = await GetAsync(vahedCode, ct);
        if (!a.Any) throw new RoleAccessDeniedException("برای کار با ماژول فروش، مدیر مالی واحد باید به شما نقش فروش بدهد.");
        return a;
    }

    public async Task<SalesAccessInfo> RequireManageAsync(string vahedCode, CancellationToken ct)
    {
        var a = await GetAsync(vahedCode, ct);
        if (!a.CanManage) throw new RoleAccessDeniedException("تغییر تنظیمات و اطلاعات پایهٔ فروش فقط برای مدیر فروش یا مدیر مالی واحد است.");
        return a;
    }

    public static string RoleTitle(SalesRole r) => r switch
    {
        SalesRole.Seller => "کارشناس فروش",
        SalesRole.Manager => "مدیر فروش",
        _ => "حسابدار فروش",
    };
}
