using System.Globalization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;

namespace Accounting.Infrastructure.Repositories;

/// <summary>مشخصات یک واحد برای تفکیک استان / گروه واحد / واحد در گزارش‌ها.</summary>
internal sealed record ReportUnitInfo(string Name, string CategoryCode, string CategoryName, string ProvinceCode, string ProvinceName)
{
    /// <summary>کد «نامشخص» برای واحدی که استان یا گروهش پیدا نشد — تا همان ردیف هم قابل پیمایش باشد.</summary>
    public const string UnknownCode = "0";

    public const string UnknownProvince = "استان نامشخص";

    public const string UnknownCategory = "گروه نامشخص";

    /// <summary>
    /// همهٔ واحدها: نام، گروه (از نوع واحد، <see cref="UnitCategories"/>) و استان (از شهر واحد). view گزارش‌ها
    /// فقط <c>VAHEDCODE</c> دارد، پس تفکیک استان/گروه همیشه «جمع به تفکیک واحد در SQL، سپس نگاشت در حافظه» است.
    /// </summary>
    public static async Task<IReadOnlyDictionary<string, ReportUnitInfo>> LoadAsync(
        IUnitAccessReadRepository? units, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, ReportUnitInfo>(StringComparer.Ordinal);
        if (units is null)
        {
            return result;
        }

        var all = await units.GetAllUnitsAsync(cancellationToken);
        var provinces = await units.GetUnitProvincesAsync(cancellationToken);
        foreach (var u in all)
        {
            var category = UnitCategories.Of(u.TypeCode);
            provinces.TryGetValue(u.VahedCode, out var p);
            result.TryAdd(u.VahedCode, new ReportUnitInfo(
                u.VahedName,
                category is { } c ? ((int)c).ToString(CultureInfo.InvariantCulture) : UnknownCode,
                category is { } c2 ? UnitCategories.Label(c2) : UnknownCategory,
                p?.ProvinceCode ?? UnknownCode,
                p?.ProvinceName ?? UnknownProvince));
        }

        return result;
    }

    /// <summary>(کد، عنوان) یک واحد در بُعد استان / گروه / واحد.</summary>
    public static (string Code, string Name) Key(IReadOnlyDictionary<string, ReportUnitInfo> info, string vahedCode, UnitDimension dimension)
    {
        info.TryGetValue(vahedCode, out var i);
        return dimension switch
        {
            UnitDimension.Province => (i?.ProvinceCode ?? UnknownCode, i?.ProvinceName ?? UnknownProvince),
            UnitDimension.Category => (i?.CategoryCode ?? UnknownCode, i?.CategoryName ?? UnknownCategory),
            _ => (vahedCode, i?.Name ?? string.Empty),
        };
    }
}

internal enum UnitDimension
{
    Province,
    Category,
    Unit,
}
