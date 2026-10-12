namespace Accounting.Application.AuditLogs;

/// <param name="Table">نام جدول اوراکل.</param>
/// <param name="Title">عنوان فارسی برای نمایش.</param>
/// <param name="LabelColumns">ستون‌هایی که رکورد را برای انسان مشخص می‌کنند (مثلاً شمارهٔ سند) — در <c>RECORDLABEL</c>.</param>
/// <param name="ParentColumn">ستون شناسهٔ والد (مثلاً ردیف ← سرسند) تا تاریخچهٔ سند ردیف‌هایش را هم نشان دهد.</param>
/// <param name="ParentTable">جدول والد؛ اگر خودش والد داشته باشد، والدِ بالاترین سطح در <c>PARENTID</c> می‌نشیند.</param>
public sealed record AuditTableInfo(
    string Table,
    string Title,
    IReadOnlyList<string> LabelColumns,
    string? ParentColumn = null,
    string? ParentTable = null);

/// <summary>
/// جدول‌های شناخته‌شده برای لاگ تغییرات. جدولی که اینجا نباشد هم ثبت می‌شود — فقط با نام خامش و بدون برچسب/والد.
/// </summary>
public static class AuditTableCatalog
{
    /// <summary>ستون‌های ردیابی که خودِ ردیف لاگ (کاربر و ساعت) جایشان را می‌گیرد؛ در «اصلاح» جدا ثبت نمی‌شوند.</summary>
    public static readonly HashSet<string> HousekeepingColumns = new(StringComparer.Ordinal)
    {
        "ADDUSERID", "CHANGEUSERID", "CREATEDDATE", "UPDATEDDATE",
    };

    /// <summary>
    /// جدول‌هایی که ثبت نمی‌شوند: خودِ لاگ، و خروجی‌های محاسبه‌شدهٔ صورت مالی (هر بار تهیه هزاران ردیف؛ ورودی کاربر نیستند
    /// و دوباره از سند ساخته می‌شوند). ورودی‌های دستی صورت (TB_FS_RUN_MANUAL، یادداشت، اقدام) ثبت می‌شوند.
    /// </summary>
    public static readonly HashSet<string> ExcludedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "TB_AUDITLOG", "TB_FS_RUN_ROW", "TB_FS_RUN_ACCOUNT", "TB_FS_RUN_STATEMENT", "TB_FS_RUN_CHECK",
    };

    private static AuditTableInfo T(string table, string title, params string[] labels) => new(table, title, labels);

    private static AuditTableInfo Child(string table, string title, string parentColumn, string parentTable, params string[] labels)
        => new(table, title, labels, parentColumn, parentTable);

    public static readonly IReadOnlyList<AuditTableInfo> All =
    [
        T("TB_VOUCHERSHEAD", "سند حسابداری", "DOC_NUM", "ATF_NUM"),
        Child("TB_VOUCHERSDETAIL", "ردیف سند", "VOUCHERSHEAD_ID", "TB_VOUCHERSHEAD"),
        Child("TB_VOUCHERDETAIL_LINK_TAFSILI", "تفصیلی ردیف سند", "VOUCHERSDETAIL_ID", "TB_VOUCHERSDETAIL"),
        Child("TB_ATTRIBSINVOUCHER", "شناسهٔ ردیف سند", "VOUCHERSDETAIL_ID", "TB_VOUCHERSDETAIL"),
        Child("TB_IDENTITYDETAIL", "ویژگی ردیف سند", "VOUCHERSDETAIL_ID", "TB_VOUCHERSDETAIL"),
        T("TB_TMP_VOUCHERHEAD", "سند دریافتی از سایر سیستم‌ها"),
        Child("TB_TMP_VOUCHERSDETAIL", "ردیف سند دریافتی", "TMPVOUCHERHEAD_ID", "TB_TMP_VOUCHERHEAD"),

        T("TB_ELAMHEAD", "اعلامیه", "ELAMH_SERIALNO", "ELAMH_CODE"),
        Child("TB_ELAMDETAIL", "ردیف اعلامیه", "ELAMHEAD_ID", "TB_ELAMHEAD"),
        Child("TB_ELAMDETAIL_LINK_TAFSILI", "تفصیلی ردیف اعلامیه", "ELAMDETAIL_ID", "TB_ELAMDETAIL"),

        T("TB_PAYRECIVHEAD", "دریافت و پرداخت", "PAYRECIVCODE"),
        Child("TB_PAYRECIVDETAIL", "ردیف دریافت و پرداخت", "PAYRECIVHEAD_ID", "TB_PAYRECIVHEAD"),
        Child("TB_PAYRECIVDETAIL_LINK_TAFSILI", "تفصیلی ردیف دریافت و پرداخت", "PAYRECIVDETAIL_ID", "TB_PAYRECIVDETAIL"),

        T("TB_BANKCARTDETAIL", "کارت حساب جاری", "CHEQNO"),
        T("TB_BANK_LIST", "بانک"),
        T("TB_BANKBRANCH_LIST", "شعبهٔ بانک"),
        T("TB_ACCOUNT", "حساب بانکی"),
        T("TB_CHECKBOOK", "دسته‌چک", "CHECKBOOK_TITLE"),
        Child("TB_CHECK", "برگ چک", "CHECKBOOK_ID", "TB_CHECKBOOK", "CHEQ_NO"),
        T("TB_CHECK_APPROVAL", "تأیید چک"),
        T("TB_CHECK_APPROVAL_EVENT", "رویداد تأیید چک"),
        T("TB_CHECK_TYPE", "نوع چک"),
        T("TB_CHEQUES_INCORRENT", "چک صوری"),
        T("TB_RECEIP", "رسید", "RECEIPT_NO"),
        T("TB_BILL_LOG", "صورتحساب"),

        T("TB_ACCOUNTCODE", "کدینگ حسابداری", "ACCCODE", "ACCCODENAME"),
        T("TB_ACCOUNTCODE_INTERFACE", "رابط حساب"),
        T("TB_ACCOUNTEXCEPTION", "استثنای حساب"),
        T("TB_ACCOUNT_LINK_LEVEL", "اتصال حساب به سطح"),
        T("TB_ACCOUNT_LINK_TAFSILGROUP", "اتصال حساب به گروه تفصیلی"),
        T("TB_ACCOUNT_LINK_TAFSILI", "اتصال حساب به تفصیلی"),
        T("TB_ACCOUNT_TYPE", "نوع حساب"),
        T("TB_TAFSILI", "تفصیلی", "TAFSILI_CODE", "TAFSILI_NAME"),
        T("TB_TAFSILI_UNITACCESS", "دسترسی واحد به تفصیلی"),
        T("TB_TAFSIL_GROUP", "گروه تفصیلی"),
        T("TB_TAFSIL_LINK_TAFSILGROUP", "اتصال تفصیلی به گروه"),
        T("TB_LEVEL_TAFSIL", "سطح تفصیلی"),
        T("TB_ATTRIBFORACCOUNTCODE", "حساب شناسه‌دار"),
        T("TB_IDENTITYGROUP", "گروه ویژگی"),
        T("TB_IDENTITYSUBGRP", "زیرگروه ویژگی"),
        T("TB_IDENTITYHEAD", "ویژگی"),
        T("TB_IDENTITYFIXITEM", "مقدار ثابت ویژگی"),
        T("TB_WHITEANDBLACKLIST", "لیست سیاه و سفید"),
        T("TB_WHITELIST", "لیست سفید"),
        T("TB_PREDESCRIB", "شرح پیش‌فرض"),
        T("TB_RABET", "رابط"),
        T("TB_RABET_TYPE", "نوع رابط"),
        T("TB_RABET_CLOSING", "رابط اختتامیه"),
        T("TB_WORKSHOP", "کارگاه"),
        T("TB_WORKSHOP_LINK_TAFSILI", "تفصیلی کارگاه"),
        T("TB_EXPENCE", "هزینه"),
        T("TB_EXPENCEGROUP", "گروه هزینه"),
        T("TB_EXPENCE_LINK_TAFSILI", "تفصیلی هزینه"),
        T("TB_PERSON_ACTION", "اقدام‌کننده"),
        T("TB_SYSTYPE", "نوع سیستم"),
        T("TB_VAHED_INFO", "واحد"),
        T("TB_VAHED_TYPE", "نوع واحد"),
        T("TB_YEAR", "سال مالی"),
        T("TB_CITY", "شهر"),
        T("TB_PROVINCE", "استان"),
        T("TB_ROLE_MENU_ACCESS", "دسترسی نقش به منو"),
        T("TB_MONTH_REOPEN", "برگشت صورتحساب ماه"),
        T("TB_MONTH_CLOSE_LOG", "صورتحساب ماه"),
        T("TB_ATTACH", "پیوست"),

        T("TB_REVOLVING_FUND", "تنخواه‌گردان", "CODE", "NAME"),
        T("TB_REVOLVINGFUND_LINK_TAFSILI", "تفصیلی تنخواه‌گردان"),
        T("TB_CHARGEANDCOST_HEAD", "صورت‌هزینه"),
        Child("TB_CHARGEANDCOST_DETAIL", "ردیف صورت‌هزینه", "CHARGEANDCOSTHEAD_ID", "TB_CHARGEANDCOST_HEAD"),
        T("TB_CHARGE_LINK_COST", "اتصال شارژ و هزینه"),
        T("TB_PC_FUND", "تنخواه", "CODE", "NAME"),
        T("TB_PC_FUND_LINK_TAFSILI", "تفصیلی تنخواه"),
        T("TB_PC_EXPENSE_DOC", "سند هزینهٔ تنخواه", "INVOICE_NO"),
        T("TB_PC_ATTACHMENT", "پیوست تنخواه"),
        T("TB_PC_DOC_EVENT", "رویداد تنخواه"),
        T("TB_PC_REFUND", "استرداد تنخواه"),
        T("TB_PC_REPLENISHMENT", "ترمیم تنخواه"),
        T("TB_PC_REVIEWER", "بررسی‌کنندهٔ تنخواه"),
        T("TB_PC_SETTLEMENT_PERIOD", "دورهٔ تسویهٔ تنخواه"),

        T("TB_TR_PAYMENT_REQUEST", "درخواست پرداخت", "CODE"),
        Child("TB_TR_PAYMENT_REQUEST_LINK_TAFSILI", "تفصیلی درخواست پرداخت", "PAYMENT_REQUEST_ID", "TB_TR_PAYMENT_REQUEST"),
        T("TB_TR_PAYMENT_REQUEST_EVENT", "رویداد درخواست پرداخت"),
        T("TB_TR_RECEIPT", "دریافت خزانه"),
        T("TB_TR_TRANSFER", "انتقال خزانه"),
        T("TB_TR_TRANSFER_EVENT", "رویداد انتقال"),
        T("TB_TR_ROLE", "نقش خزانه"),
        T("TB_TR_SETTING", "تنظیمات خزانه"),
        T("TB_TR_BANK_STATEMENT", "صورت‌حساب بانک"),
        Child("TB_TR_BANK_STATEMENT_LINE", "ردیف صورت‌حساب بانک", "STATEMENT_ID", "TB_TR_BANK_STATEMENT"),

        T("TB_FS_TEMPLATE", "قالب صورت مالی"),
        T("TB_FS_TEMPLATE_VERSION", "نسخهٔ قالب صورت مالی"),
        T("TB_FS_TEMPLATE_ROW", "ردیف قالب صورت مالی", "CODE"),
        T("TB_FS_RUN", "تهیهٔ صورت مالی"),
        T("TB_FS_PERIOD", "دورهٔ صورت مالی"),
        T("TB_FS_PERMISSION", "دسترسی صورت مالی"),
        T("TB_FS_APPROVAL_STEP", "گام تأیید صورت مالی"),
        T("TB_FS_CHECK_RULE", "کنترل صورت مالی"),
        T("TB_FS_NARRATIVE", "یادداشت توضیحی"),
        T("TB_FS_CONSOLIDATION", "تلفیق"),
        T("TB_FS_RATIO", "نسبت مالی"),
    ];

    private static readonly Dictionary<string, AuditTableInfo> ByTable =
        All.ToDictionary(t => t.Table, StringComparer.OrdinalIgnoreCase);

    public static AuditTableInfo? Find(string table) => ByTable.GetValueOrDefault(table);

    public static string TitleOf(string table) => Find(table)?.Title ?? table;
}
