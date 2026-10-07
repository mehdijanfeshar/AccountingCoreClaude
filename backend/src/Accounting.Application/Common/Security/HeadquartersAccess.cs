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
