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

    /// <summary>فقط نقش لاگ تغییرات (<see cref="AppRoles.Audit"/>) — مدیر ستاد هم خودکار ندارد.</summary>
    Audit,
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
    private const string Sales = "فروش";
    private const string Operation = "عملیات";
    private const string Reports = "گزارش‌ها";
    private const string Treasury = "تنخواه و خزانه‌داری";
    private const string Fs = "صورت‌های مالی";

    /// <summary>منوی لاگ تغییرات؛ مدیر ستاد هم فقط با تخصیص صریح در «دسترسی نقش‌ها» می‌بیند.</summary>
    public const string AuditLogKey = "/reports/audit-log";

    private static MenuDefinition M(string key, string group, string title, MenuGroupAccess access, params string[] modules)
        => new(key, group, title, access, modules);

    public static readonly IReadOnlyList<MenuDefinition> All =
    [
        M("/base/account-codes", Base, "کدینگ حسابداری", MenuGroupAccess.OperateOrNational,
            "Accounts", "AccountCodes", "AccountCodeInterfaces", "AccountExceptions", "Tafsilis"),
        M("/base/year-end-settings", Base, "تعریف رابط‌ها", MenuGroupAccess.OperateOrNational,
            "Rabets", "AccountCodeInterfaces", "RabetClosings", "AccountExceptions"),
        M("/base/tafsil-groups", Base, "گروه تفصیلی", MenuGroupAccess.OperateOrNational, "TafsilGroups"),
        M("/base/level-tafsils", Base, "سطوح تفصیلی", MenuGroupAccess.OperateOrNational, "LevelTafsils"),
        M("/base/bank", Base, "بانک", MenuGroupAccess.OperateOrNational, "BankAccounts", "CheckBooks", "ChequeTypes"),
        M("/base/features", Base, "ویژگی", MenuGroupAccess.OperateOrNational, "IdentityGroups", "IdentitySubGroups", "IdentityHeads"),
        M("/base/attrib-for-account-codes", Base, "حساب‌های شناسه‌دار", MenuGroupAccess.OperateOrNational, "AttribForAccountCodes"),
        M("/base/coding-permissions", Base, "دسترسی کدینگ حسابداری", MenuGroupAccess.OperateOrNational, "WhiteAndBlackLists", "WhiteLists"),
        M("/base/work-shops", Base, "کارگاه", MenuGroupAccess.OperateOrNational, "WorkShops"),

        M("/assistant", Assistant, "ثبت سند با حسابیار", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/batch", Assistant, "ثبت گروهی از اکسل", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/history", Assistant, "سندهای حسابیار", MenuGroupAccess.Operate),
        M("/assistant/review", Assistant, "کارتابل بررسی حسابیار", MenuGroupAccess.Operate),
        M("/assistant/reports", Assistant, "گزارش با حسابیار", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/reports/manage", Assistant, "گزارش‌های ذخیره‌شده", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/templates", Assistant, "الگوهای عملیات", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/templates/discover", Assistant, "کشف الگو از اسناد گذشته", MenuGroupAccess.Operate, "OperationTemplates"),
        M("/assistant/chat", Assistant, "گفتگو با حسابیار", MenuGroupAccess.Operate),
        M("/assistant/ai-eval", Assistant, "سنجش هوش مصنوعی", MenuGroupAccess.Operate),

        M("/sales", Sales, "میز کار فروش", MenuGroupAccess.Operate),
        M("/sales/docs", Sales, "اسناد فروش", MenuGroupAccess.Operate, "Sales"),
        M("/sales/customers", Sales, "مشتریان", MenuGroupAccess.Operate),
        M("/sales/items", Sales, "کالا و خدمات", MenuGroupAccess.Operate),
        M("/sales/settings", Sales, "تنظیمات فروش", MenuGroupAccess.Operate),

        M("/operation/voucher-heads", Operation, "کارتابل اسناد", MenuGroupAccess.Operate, "Vouchers"),
        M("/operation/vouchers/new", Operation, "صدور سند", MenuGroupAccess.Operate, "Vouchers"),
        M("/operation/year-end", Operation, "سند افتتاحیه و اختتامیه", MenuGroupAccess.Operate, "Vouchers"),
        M("/operation/elams", Operation, "اسناد اعلامیه", MenuGroupAccess.Operate, "Elams", "ElamHeads"),
        M("/operation/bank-card", Operation, "کارت حساب جاری", MenuGroupAccess.Operate, "BankCards", "BankCartDetails"),
        M("/operation/legacy-import", Operation, "انتقال از سیستم قدیم", MenuGroupAccess.Operate, "LegacyImport"),
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
        M(AuditLogKey, Reports, "مشاهده لاگ تغییرات", MenuGroupAccess.Audit),

        M("/treasury/petty-cash/dashboard", Treasury, "داشبورد تنخواه", MenuGroupAccess.Operate),
        M("/treasury/petty-cash/cartable", Treasury, "کارتابل تنخواه", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/expense-docs/new", Treasury, "ثبت صورت‌هزینه", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/replenishments", Treasury, "شارژ و ترمیم", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/settlement", Treasury, "تسویه دوره", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/ledger", Treasury, "گزارش گردش تنخواه", MenuGroupAccess.Operate),
        M("/treasury/petty-cash/funds", Treasury, "تعریف تنخواه", MenuGroupAccess.Operate, "PettyCash"),
        M("/treasury/petty-cash/expenses", Treasury, "تعریف هزینه", MenuGroupAccess.Operate, "Expenses"),
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
        M("/fs/intercompany", Fs, "تلفیق و حذف فی‌مابین", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/narratives", Fs, "یادداشت‌های توضیحی", MenuGroupAccess.Any, "FinancialStatements"),
        M("/fs/analysis", Fs, "تحلیل و نسبت‌ها", MenuGroupAccess.Any),
    ];

    /// <summary>
    /// تب‌های قابل تخصیص جدا (زیر منوی دارای تب). مقدار <c>TabKey</c> باید با <c>value</c> هر <c>Tab</c> در فرانت یکی باشد.
    /// ماژول هر تب فقط وقتی نوشتن را مجاز می‌کند که سطح همان تب «ثبت و تغییر» باشد؛ تب‌هایی که یک ماژول مشترک دارند
    /// (گروه/کل/معین = <c>Accounts</c>) در سرور فقط در سطح ماژول جدا می‌شوند و جداسازی‌شان با فرانت است.
    /// </summary>
    public static readonly IReadOnlyList<MenuTabDefinition> Tabs =
    [
        new("/base/account-codes", "group", "گروه", ["Accounts"]),
        new("/base/account-codes", "kol", "کل", ["Accounts"]),
        new("/base/account-codes", "moin", "معین", ["Accounts"]),
        new("/base/account-codes", "tafsili", "تفصیلی", ["Tafsilis"]),
        new("/base/account-codes", "moinTafsiliLink", "ارتباط معین با گروه تفصیلی", ["Accounts"]),
        new("/base/bank", "accounts", "حساب‌های بانکی", ["BankAccounts"]),
        new("/base/bank", "checkBooks", "دسته‌چک", ["CheckBooks"]),
        new("/base/bank", "chequeSettings", "تنظیمات محیطی چک", ["ChequeTypes"]),
        new("/base/features", "groups", "گروه ویژگی", ["IdentityGroups"]),
        new("/base/features", "parts", "اجزای ویژگی", ["IdentitySubGroups"]),
        new("/base/features", "records", "ویژگی‌های ثبت‌شده", ["IdentityHeads"]),
        new("/base/year-end-settings", "elam", "رابط اعلامیه", ["Rabets"]),
        new("/base/year-end-settings", "interfaces", "رابط افتتاحیه/اختتامیه", ["AccountCodeInterfaces"]),
        new("/base/year-end-settings", "rabet", "رابط اختتامیه (گروه ۶، ۷، ۸)", ["RabetClosings"]),
        new("/base/year-end-settings", "exceptions", "حساب‌های مستثنا", ["AccountExceptions"]),
        // انواع سند و اعلامیه و دریافت/انتقال ماژول Application مشترک دارند؛ جداسازی‌شان فقط در فرانت است.
        new("/operation/year-end", "closing", "سند اختتامیه", ["Vouchers"]),
        new("/operation/year-end", "opening", "سند افتتاحیه", ["Vouchers"]),
        new("/operation/elams", "sent", "سایر اعلامیه‌ها — صادره", ["Elams", "ElamHeads"]),
        new("/operation/elams", "received", "سایر اعلامیه‌ها — رسیده", ["Elams", "ElamHeads"]),
        new("/operation/elams", "revenue", "اعلامیهٔ درآمد", ["Elams", "ElamHeads"]),
        new("/treasury/khazaneh/receipts-transfers", "receipts", "دریافت وجه", ["Treasury", "Receipts"]),
        new("/treasury/khazaneh/receipts-transfers", "transfers", "انتقال وجه", ["Treasury", "Receipts"]),
    ];

    private static readonly Dictionary<string, MenuDefinition> ByKey = All.ToDictionary(m => m.Key, StringComparer.Ordinal);

    private static readonly Dictionary<string, MenuTabDefinition> TabByKey = Tabs.ToDictionary(t => t.Key, StringComparer.Ordinal);

    public static bool Contains(string key) => ByKey.ContainsKey(key);

    public static bool ContainsTab(string key) => TabByKey.ContainsKey(key);

    /// <summary>کلید منوی والد یک کلید تب (<c>menu#tab</c>)؛ null اگر کلید تب نیست.</summary>
    public static string? ParentMenuKey(string key)
    {
        var i = key.IndexOf(MenuTabDefinition.Separator);
        return i > 0 ? key[..i] : null;
    }

    /// <summary>منوهایی که «ثبت و تغییر»شان نوشتن روی این ماژول را مجاز می‌کند.</summary>
    public static IReadOnlyList<MenuDefinition> MenusForModule(string module)
        => All.Where(m => m.Modules.Contains(module, StringComparer.Ordinal)).ToList();

    /// <summary>
    /// کلیدهایی که «ثبت و تغییر»شان نوشتن روی این ماژول را مجاز می‌کند: اگر منو برای ماژول تب دارد فقط همان تب‌ها،
    /// وگرنه خود منو (سطح تبِ بی‌ردیف از منو ارث می‌رسد).
    /// </summary>
    public static IReadOnlyList<string> WriteKeysForModule(string module)
        => MenusForModule(module)
            .SelectMany(m =>
            {
                var tabs = Tabs.Where(t => t.MenuKey == m.Key && t.Modules.Contains(module, StringComparer.Ordinal)).ToList();
                return tabs.Count > 0 ? tabs.Select(t => t.Key) : [m.Key];
            })
            .ToList();

    /// <summary>
    /// سطح پیش‌فرض یک نقش ثابت در یک منو، عین رفتار پیش از فاز ۵۴: دیده‌شدن گروه طبق <see cref="MenuDefinition.Access"/>،
    /// و «ثبت و تغییر» وقتی <see cref="AppRoles.AllowedFor"/> نوشتن همهٔ ماژول‌های منو را به نقش بدهد.
    /// </summary>
    public static int DefaultLevel(string role, MenuDefinition menu) => DefaultLevel(role, menu, menu.Modules);

    /// <summary>پیش‌فرض یک تب: دیده‌شدن از منوی والد، و «ثبت و تغییر» فقط با ماژول‌های خود تب.</summary>
    public static int DefaultLevel(string role, MenuTabDefinition tab)
        => DefaultLevel(role, ByKey[tab.MenuKey], tab.Modules);

    private static int DefaultLevel(string role, MenuDefinition menu, IReadOnlyList<string> writeModules)
    {
        var operate = AppRoles.OperationWriters.Contains(role);
        var visible = menu.Access switch
        {
            MenuGroupAccess.Operate => operate,
            MenuGroupAccess.OperateOrNational => operate || role == AppRoles.National,
            MenuGroupAccess.Audit => role == AppRoles.Audit,
            _ => AppRoles.All.Contains(role),
        };
        if (!visible)
            return RoleMenuAccessLevels.None;
        return writeModules.Count > 0 && writeModules.All(module => AppRoles.AllowedFor(module, isQuery: false).Contains(role))
            ? RoleMenuAccessLevels.Edit
            : RoleMenuAccessLevels.View;
    }
}

/// <param name="MenuKey">کلید منوی والد (<see cref="MenuDefinition.Key"/>).</param>
/// <param name="TabKey"><c>value</c> تب در فرانت.</param>
public sealed record MenuTabDefinition(string MenuKey, string TabKey, string Title, IReadOnlyList<string> Modules)
{
    public const char Separator = '#';

    /// <summary>کلید ذخیره در <c>TB_ROLE_MENU_ACCESS.MENU_KEY</c>: <c>/base/bank#checkBooks</c>.</summary>
    public string Key => MenuKey + Separator + TabKey;
}

public static class RoleMenuAccessLevels
{
    public const int None = 0;
    public const int View = 1;
    public const int Edit = 2;
}
