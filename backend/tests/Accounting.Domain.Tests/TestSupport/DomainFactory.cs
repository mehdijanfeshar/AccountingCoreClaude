using System.Threading;
using Accounting.Domain.Entities;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Tests.TestSupport;

/// <summary>
/// سازنده‌های کمکی برای ساخت سریع سلسله‌مراتب گروه/کل/معین و انواع/مقادیر تفصیلی
/// در تست‌های واحد دامنه، بدون نیاز به دیتابیس. کدهای گروه/کل/معین به‌صورت پیش‌فرض
/// طبق طول ثابت هر سطح (۱/۲/۳ رقم) تولید می‌شوند تا AccountCode.Create رد نکند.
/// </summary>
public static class DomainFactory
{
    private static int _seq = 1000;

    private static int NextId() => Interlocked.Increment(ref _seq);

    /// <summary>یک گروه با یک کل و یک معین زیر آن می‌سازد (بدون هیچ لینک تفصیلی).</summary>
    public static (AccountGroup Group, GeneralLedgerAccount Gl, SubsidiaryAccount Sub) CreateHierarchy(
        AccountNature nature = AccountNature.Debit,
        string groupCode = "1",
        string glCode = "10",
        string subCode = "100",
        string subTitle = "صندوق مرکزی")
    {
        var group = AccountGroup.Create(groupCode, "دارایی‌ها");
        var gl = group.AddGeneralLedgerAccount(glCode, "موجودی نقد و بانک");
        var sub = gl.AddSubsidiaryAccount(subCode, subTitle, nature);
        return (group, gl, sub);
    }

    /// <summary>یک DetailAccountType با Id متمایز (شبیه‌سازی EF) می‌سازد.</summary>
    public static DetailAccountType CreateDetailType(string title)
        => EntityIdAssigner.WithId(DetailAccountType.Create(title), NextId());

    /// <summary>یک DetailAccount متعلق به نوع مشخص، با Id متمایز، می‌سازد.</summary>
    public static DetailAccount CreateDetailAccount(DetailAccountType type, string code, string title)
        => EntityIdAssigner.WithId(DetailAccount.Create(type, code, title), NextId());
}
