using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tafsilis.Commands.Common;

/// <summary>
/// دامنهٔ تفصیلی (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷). «مالکیت»، «نوع واحد» و «دامنهٔ دیده‌شدن گروه‌ها» فقط برای
/// کاربر ستاد مرکزی قابل انتخاب است:
/// <list type="bullet">
/// <item>کاربر واحد: مالکیت = داخلی، نوع واحد = درمان/بیمه از گروه واحد خودش (ستادی = نامشخص)، ارتباط با
/// گروه با کد واحد خودش و بدون نوع واحد — یعنی فقط خودش می‌بیند. مقادیر ارسالی کلاینت نادیده گرفته می‌شوند.</item>
/// <item>ستاد: مقادیر خودش؛ اگر دامنه (نوع واحد) انتخاب شود، ارتباط با گروه با نوع واحد و بدون کد واحد ثبت
/// می‌شود؛ بدون دامنه، فقط برای ستاد (کد واحد ستاد).</item>
/// </list>
/// کد واحدِ خود ردیف تفصیلی (<c>TB_TAFSILI.VAHEDCODE</c>) همیشه واحد سازنده می‌ماند (فهرست و مالکیت رکورد).
/// </summary>
public static class TafsiliScopePolicy
{
    public sealed record Scope(Owners? Owner, VahedCategory? VahedType, short? LinkVahedType, string? LinkVahedCode);

    public static async Task<Scope> ResolveAsync(
        ICurrentUser user,
        IUnitAccessReadRepository unitAccess,
        string vahedCode,
        Owners? requestedOwner,
        VahedCategory? requestedVahedType,
        VahedCategory? requestedLinkVahedType,
        CancellationToken ct)
    {
        if (await HeadquartersAccess.IsHeadquartersAdminAsync(user, unitAccess, ct))
        {
            var linkType = requestedLinkVahedType;
            return new Scope(
                requestedOwner ?? (linkType is null ? Owners.Unit : Owners.Global),
                requestedVahedType ?? linkType,
                linkType is { } t ? (short)t : null,
                linkType is null ? vahedCode : null);
        }

        var category = await HeadquartersAccess.CategoryOfAsync(vahedCode, unitAccess, ct);
        VahedCategory? ownType = category switch
        {
            UnitCategory.Medical => VahedCategory.Treatment,
            UnitCategory.Insurance => VahedCategory.Insurance,
            _ => null,
        };
        return new Scope(Owners.Unit, ownType, null, vahedCode);
    }
}
