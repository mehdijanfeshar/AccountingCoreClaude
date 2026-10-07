namespace Accounting.Application.Common.Security;

/// <summary>
/// گروه منو و قاعدهٔ دیده‌شدنش در سیستم ثابت قبلی (برای «پیش‌فرض فعلی» در صفحهٔ دسترسی نقش‌ها) — همان
/// <c>access</c> گروه در <c>navConfig.tsx</c> فرانت.
/// </summary>
public enum MenuGroupAccess
{
    /// <summary>همهٔ نقش‌ها.</summary>
    Any,

    /// <summary>فقط نقش‌های عملیاتی (<see cref="AppRoles.OperationWriters"/>).</summary>
    Operate,

    /// <summary>نقش‌های عملیاتی یا مدیریتی سطح کشور.</summary>
    OperateOrNational,
}

/// <param name="Key">کلید منو = مسیر صفحه در فرانت (<c>to</c> در navConfig).</param>
/// <param name="Modules">
/// ماژول‌های Application (بخش سوم namespace) که نوشتن‌شان با «ثبت و تغییر» این منو مجاز می‌شود. خالی = منوی فقط
/// خواندنی (گزارش). یک ماژول می‌تواند زیر چند منو باشد؛ «ثبت و تغییر» در هر کدام کافی است.
/// </param>
public sealed record MenuDefinition(string Key, string Group, string Title, MenuGroupAccess Access, IReadOnlyList<string> Modules)
{
    public bool HasWrite => Modules.Count > 0;
}

/// <summary>
/// فهرست منوهای قابل تخصیص به نقش (فاز ۵۴). باید با <c>navConfig.tsx</c> فرانت هم‌خوان بماند: منوی تازه‌ای که اینجا
/// نیاید، تابع قاعدهٔ ثابت قبلی می‌ماند. صفحهٔ «دسترسی نقش‌ها» خودش اینجا نیست — همیشه فقط مدیر ستاد.
/// </summary>
public static class MenuCatalog
{
    private const string Base = "اطلاعات پایه";
    private const string Assistant = "حسابیار";
    private const string Operation = "عملیات";
    private const string Reports = "گزارش‌ها";
    private const string Treasury = "تنخواه و خزانه‌داری";
    private const string Fs = "صورت‌های مالی";

    private static MenuDefinition M(string key, string group, string title, MenuGroupAccess access, params string[] modules)
        => new(key, group, title, access, modules);

    public static readonly IReadOnlyList<MenuDefinition> All =
    [
        M("/base/account-codes", Base, "کدینگ حسابداری", MenuGroupAccess.OperateOrNational,
            "Accounts", "AccountCodes", "AccountCodeInterfaces", "AccountExceptions", "Tafsilis"),
        M("/base/tafsil-groups", Base, "گروه تفصیلی", MenuGroupAccess.OperateOrNational, "TafsilGroups"),
        M("/base/level-tafsils", Base, "سطوح تفصیلی", MenuGroupAccess.OperateOrNational, "LevelTafsils"),
        M("/base/bank", Base, "بانک", MenuGroupAccess.OperateOrNational, "BankAccounts", "CheckBooks", "ChequeTypes"),
        M("/base/features", Base, "ویژگی", MenuGroupAccess.OperateOrNational, "IdentityGroups", "IdentitySubGroups", "IdentityHeads"),
        M("/base/attrib-for-account-codes", Base, "حساب‌های شناسه‌دار", MenuGroupAccess.OperateOrNational, "AttribForAccountCodes"),
        M("/base/coding-permissions", Base, "دسترسی کدینگ حسابداری", MenuGroupAccess.OperateOrNational, "WhiteAndBlackLists", "WhiteLists"),
        M("/base/work-shops", Base, "کارگاه", MenuGroupAccess.OperateOrNational, "WorkShops"),

        M("/assistant", Assistant, "ثبت سند با حسابیار", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/history", Assistant, "سندهای حسابیار", MenuGroupAccess.Operate),
        M("/assistant/reports", Assistant, "گزارش با حسابیار", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/reports/manage", Assistant, "گزارش‌های ذخیره‌شده", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/templates", Assistant, "الگوهای عملیات", MenuGroupAccess.Operate, "OperationTemplates"),

        M("/operation/voucher-heads", Operation, "کارتابل اسناد", MenuGroupAccess.Operate, "Vouchers"),
        M("/operation/vouchers/new", Operation, "صدور سند", MenuGroupAccess.Operate, "Vouchers"),
        M("/operation/elams", Operation, "اسناد اعلامیه", MenuGroupAccess.Operate, "Elams", "ElamHeads"),
        M("/operation/bank-card", Operation, "کارت حساب جاری", MenuGroupAccess.Operate, "BankCards", "BankCartDetails"),
        M("/operation/cheque-book", Operation, "دفتر چک", MenuGroupAccess.Operate, "ChequeBook"),
        M("/operation/system-voucher-inbox", Operation, "دریافت اسناد از سایر سیستم‌ها", MenuGroupAccess.Operate, "Vouchers", "TmpVoucherHeads"),
        M("/operation/month-close", Operation, "صورتحساب ماه", MenuGroupAccess.Operate, "MonthReopen"),
        M("/operation/month-reopen", Operation, "برگشت صورتحساب ماه", MenuGroupAccess.Operate, "MonthReopen"),
        M("/operation/month-reopen/issue", Operation, "صدور رمز برگشت صورتحساب", MenuGroupAccess.Operate, "MonthReopen"),

        M("/reports/trial-balance", Reports, "تراز آزمایشی", MenuGroupAccess.Any),
        M("/reports/general-ledger", Reports, "دفتر کل", MenuGroupAccess.Any),
        M("/reports/account-journal", Reports, "دفتر روزنامه", MenuGroupAccess.Any),
        M("/reports/voucher-review", Reports, "مرور اسناد", MenuGroupAccess.Any),
        M("/reports/account-review", Reports, "مرور حساب‌ها", MenuGroupAccess.Any),
        M("/reports/attribute-accounts", Reports, "مغایرت‌گیری حساب‌های شناسه‌دار", MenuGroupAccess.Any),
        M("/reports/matrix", Reports, "گزارش ماتریسی", MenuGroupAccess.Any),

        M("/treasury/petty-cash/dashboard", Treasury, "داشبورد تنخواه", MenuGroupAccess.Operate),
        M("/treasury/petty-cash/cartable", Treasury, "کارتابل تنخواه", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/expense-docs/new", Treasury, "ثبت صورت‌هزینه", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/replenishments", Treasury, "شارژ و ترمیم", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/settlement", Treasury, "تسویه دوره", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/ledger", Treasury, "گزارش گردش تنخواه", MenuGroupAccess.Operate),
        M("/treasury/petty-cash/funds", Treasury, "تعریف تنخواه", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/khazaneh/dashboard", Treasury, "داشبورد خزانه", MenuGroupAccess.Operate),
        M("/treasury/khazaneh/payment-requests", Treasury, "درخواست پرداخت", MenuGroupAccess.Operate, "Treasury"),
        M("/treasury/khazaneh/cartable", Treasury, "کارتابل تأیید", MenuGroupAccess.Operate, "Treasury"),
        M("/treasury/khazaneh/settings", Treasury, "تنظیمات خزانه", MenuGroupAccess.Operate, "Treasury"),
        M("/treasury/khazaneh/roles", Treasury, "نقش‌های خزانه", MenuGroupAccess.Operate, "Treasury"),
        M("/treasury/khazaneh/execution", Treasury, "اجرای پرداخت", MenuGroupAccess.Operate, "Treasury"),
        M("/treasury/khazaneh/receipts-transfers", Treasury, "دریافت و انتقال", MenuGroupAccess.Operate, "Treasury", "Receipts"),
        M("/treasury/khazaneh/bank-reconciliation", Treasury, "مغایرت بانکی", MenuGroupAccess.Operate, "Treasury"),

        M("/fs/dashboard", Fs, "داشبورد صورت‌ها", MenuGroupAccess.Any),
        M("/fs/runs", Fs, "تهیهٔ صورت‌های مالی", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/templates", Fs, "قالب صورت‌ها", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/account-mapping", Fs, "نگاشت حساب‌ها", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/check-rules", Fs, "کنترل‌های صورت‌ها", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/approval-steps", Fs, "گردش تأیید صورت‌ها", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/period-close", Fs, "بستن دوره", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/narratives", Fs, "یادداشت‌های توضیحی", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/analysis", Fs, "تحلیل و نسبت‌ها", MenuGroupAccess.Any),
    ];

    private static readonly Dictionary<string, MenuDefinition> ByKey = All.ToDictionary(m => m.Key, StringComparer.Ordinal);

    public static bool Contains(string key) => ByKey.ContainsKey(key);

    /// <summary>منوهایی که «ثبت و تغییر»شان نوشتن روی این ماژول را مجاز می‌کند.</summary>
    public static IReadOnlyList<MenuDefinition> MenusForModule(string module)
        => All.Where(m => m.Modules.Contains(module, StringComparer.Ordinal)).ToList();

    /// <summary>
    /// سطح پیش‌فرض یک نقش ثابت در یک منو، عین رفتار پیش از فاز ۵۴: دیده‌شدن گروه طبق <see cref="MenuDefinition.Access"/>،
    /// و «ثبت و تغییر» وقتی <see cref="AppRoles.AllowedFor"/> نوشتن همهٔ ماژول‌های منو را به نقش بدهد.
    /// </summary>
    public static int DefaultLevel(string role, MenuDefinition menu)
    {
        var operate = AppRoles.OperationWriters.Contains(role);
        var visible = menu.Access switch
        {
            MenuGroupAccess.Operate => operate,
            MenuGroupAccess.OperateOrNational => operate || role == AppRoles.National,
            _ => AppRoles.All.Contains(role),
        };
        if (!visible)
            return RoleMenuAccessLevels.None;
        return menu.HasWrite && menu.Modules.All(module => AppRoles.AllowedFor(module, isQuery: false).Contains(role))
            ? RoleMenuAccessLevels.Edit
            : RoleMenuAccessLevels.View;
    }
}

public static class RoleMenuAccessLevels
{
    public const int None = 0;
    public const int View = 1;
    public const int Edit = 2;
}
