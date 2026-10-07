namespace Accounting.Application.Common.Security;

/// <summary>قاعدهٔ پیش‌فرض یک قابلیت تا وقتی «دسترسی نقش‌ها» پیکربندی نشده (و مقدار «پر کردن با پیش‌فرض فعلی»).</summary>
public enum AbilityDefault
{
    /// <summary>فقط مدیر ستاد در واحد ستاد مرکزی.</summary>
    HeadquartersAdmin,

    /// <summary>مدیر ستاد مرکزی یا نقش مدیریتی سطح کشور.</summary>
    Country,
}

/// <param name="Key">کلید قابلیت (در جدول با پیشوند <see cref="AbilityCatalog.StoragePrefix"/>).</param>
/// <param name="Group">گروه منو در صفحهٔ دسترسی نقش‌ها.</param>
/// <param name="MenuKey">منویی که قابلیت زیرش نمایش داده می‌شود؛ null = زیر سرگروه (مثل همهٔ گزارش‌ها).</param>
public sealed record AbilityDefinition(string Key, string Group, string? MenuKey, string Title, AbilityDefault Default);

/// <summary>
/// قابلیت‌های زیرمنو (فاز ۵۴) — دسترسی به یک بخش یا دکمه داخل یک فرم، اختیاری و فقط برای فرم‌هایی که لازم دارند.
/// هر قابلیت هم در فرانت (پنهان/فعال کردن بخش) و هم در سرور (<see cref="IHeadquartersAccessService.HasAbilityAsync"/>)
/// کنترل می‌شود. برای افزودن قابلیت تازه: یک سطر این‌جا + کنترل آن در Handler + <c>hasAbility</c> در فرم.
/// مدیر ستاد مرکزی همیشه همه را دارد.
/// </summary>
public static class AbilityCatalog
{
    public const string StoragePrefix = "ability:";

    public const string TafsiliScope = "tafsili.scope";
    public const string ReportsUnitCategory = "reports.unit-category";
    public const string FsUnitCategory = "fs.unit-category";
    public const string TemplatesDefine = "templates.define";
    public const string SavedReportsDefine = "saved-reports.define";

    public static readonly IReadOnlyList<AbilityDefinition> All =
    [
        new(TafsiliScope, "اطلاعات پایه", "/base/account-codes",
            "تعیین مالکیت، نوع واحد و دامنهٔ دیده‌شدن در تعریف تفصیلی", AbilityDefault.HeadquartersAdmin),
        new(ReportsUnitCategory, "گزارش‌ها", null,
            "گزارش «همهٔ واحدهای کشور» و انتخاب گروه واحد (بیمه‌ای/درمانی/ستادی)", AbilityDefault.Country),
        new(FsUnitCategory, "صورت‌های مالی", "/fs/runs",
            "انتخاب گروه واحد (بیمه‌ای/درمانی/ستادی) در صورت ترکیبی", AbilityDefault.Country),
        new(TemplatesDefine, "حسابیار", "/assistant/templates",
            "تعریف و تغییر الگوهای عملیات (سراسری)", AbilityDefault.HeadquartersAdmin),
        new(SavedReportsDefine, "حسابیار", "/assistant/reports/manage",
            "تعریف و تغییر گزارش‌های ذخیره‌شده (سراسری)", AbilityDefault.HeadquartersAdmin),
    ];

    public static string StorageKey(string ability) => StoragePrefix + ability;

    public static bool IsStorageKey(string key) => key.StartsWith(StoragePrefix, StringComparison.Ordinal);

    public static bool Contains(string ability) => All.Any(a => a.Key == ability);

    /// <summary>مقدار «پیش‌فرض فعلی» برای یک نقش ثابت (مدیر ستاد جدا و همیشه دارد).</summary>
    public static bool DefaultFor(string role, AbilityDefinition ability)
        => ability.Default == AbilityDefault.Country && role == AppRoles.National;
}
