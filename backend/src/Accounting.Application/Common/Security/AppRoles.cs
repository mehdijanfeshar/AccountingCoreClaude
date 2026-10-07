namespace Accounting.Application.Common.Security;

/// <summary>
/// نقش‌های سامانهٔ مالی — همان نقش‌های سامانهٔ ورود سازمان (IDP) که سیستم قدیم داشت
/// (<c>Presentaion.Web.API/Roles/Roles.cs</c>)، به‌علاوهٔ نقش جدید «مدیریتی سطح کشور» (تصمیم صاحب پروژه
/// ۲۰۲۶-۱۰-۰۵). نقش‌ها از claim استاندارد role توکن خوانده می‌شوند (<see cref="Interfaces.ICurrentUser.IsInRole"/>).
/// </summary>
public static class AppRoles
{
    /// <summary>مدیر ستاد — همهٔ دسترسی‌ها در واحد خود، از جمله دادهٔ پایه و کدینگ.</summary>
    public const string SetadAdmin = "FINANCIAL CORE SETAD ADMIN";

    /// <summary>مسئول حسابداری — همهٔ عملیات داخلی واحد.</summary>
    public const string MaliAdmin = "FINANCIAL CORE MALI ADMIN";

    /// <summary>مدیر درمانی — صدور سند، کارتابل، تعریف تفصیلی، گزارش‌ها.</summary>
    public const string HltAdmin = "FINANCIAL CORE HLT ADMIN";

    /// <summary>مدیر بیمه‌ای — صدور سند، کارتابل، تعریف تفصیلی، گزارش‌ها.</summary>
    public const string EdkAdmin = "FINANCIAL CORE EDK ADMIN";

    /// <summary>کارمند حسابداری — صدور سند، کارتابل، گزارش‌ها.</summary>
    public const string User = "FINANCIAL CORE USER";

    /// <summary>گزارش‌گیری و حسابرسی — فقط مشاهده.</summary>
    public const string Report = "FINANCIAL CORE REPORT";

    /// <summary>فناوری اطلاعات — عملیات داخلی واحد و صاحبان امضا.</summary>
    public const string It = "FINANCIAL CORE IT";

    /// <summary>
    /// نقش جدید مدیریتی سطح کشور — دیدن و گزارش از همهٔ واحدها و تهیهٔ صورت مالی تجمیعی؛ در واحد
    /// دیگر فقط مشاهده (ثبت و تغییر نه). جای «نوع واحد ستاد مرکزی» را در دسترسی همه‌واحدی گرفت.
    /// </summary>
    public const string National = "FINANCIAL CORE NATIONAL";

    public static readonly string[] All = [SetadAdmin, MaliAdmin, HltAdmin, EdkAdmin, User, Report, It, National];

    /// <summary>نوشتن روی دادهٔ پایه/پیکربندی سراسری (کدینگ، گروه تفصیلی، رابط، سال، لیست سیاه و سفید…).</summary>
    public static readonly string[] ReferenceWriters = [SetadAdmin];

    /// <summary>نوشتن روی پیکربندی واحد (حساب بانکی، شناسه، هزینه، تفصیلی…).</summary>
    public static readonly string[] UnitAdminWriters = [SetadAdmin, MaliAdmin, HltAdmin, EdkAdmin, It];

    /// <summary>نوشتن عملیاتی (سند، اعلامیه، چک، خزانه، تنخواه…).</summary>
    public static readonly string[] OperationWriters = [SetadAdmin, MaliAdmin, HltAdmin, EdkAdmin, User, It];

    /// <summary>صورت‌های مالی — به‌علاوهٔ نقش مدیریتی سطح کشور (تهیهٔ صورت تجمیعی در واحد خود).</summary>
    public static readonly string[] FinancialStatementWriters = [SetadAdmin, MaliAdmin, HltAdmin, EdkAdmin, User, It, National];

    /// <summary>ماژول‌های Application که نوشتن‌شان فقط برای مدیر ستاد است (عین سیستم قدیم).</summary>
    public static readonly HashSet<string> ReferenceModules = new(StringComparer.Ordinal)
    {
        "AccountCodes", "AccountCodeInterfaces", "AccountExceptions", "TafsilGroups", "LevelTafsils", "Rabets",
        "WhiteAndBlackLists", "WhiteLists", "SysTypes", "VahedInfos", "VahedTypes", "Years",
        "IdentityGroups", "IdentitySubGroups", "RoleAccess",
    };

    /// <summary>ماژول‌هایی که نوشتن‌شان برای ادمین‌های واحد است (نه کارمند).</summary>
    public static readonly HashSet<string> UnitAdminModules = new(StringComparer.Ordinal)
    {
        "BankAccounts", "CheckBooks", "ChequeTypes", "AttribForAccountCodes", "Expenses", "Tafsilis",
        "PersonActions", "RevolvingFunds", "IdentityHeads", "Accounts",
    };

    /// <summary>نقش‌های مجاز برای یک درخواست MediatR بر اساس ماژول و خواندن/نوشتن.</summary>
    public static string[] AllowedFor(string module, bool isQuery)
    {
        if (isQuery)
            return All;
        if (module == "FinancialStatements")
            return FinancialStatementWriters;
        if (ReferenceModules.Contains(module))
            return ReferenceWriters;
        if (UnitAdminModules.Contains(module))
            return UnitAdminWriters;
        return OperationWriters;
    }
}
