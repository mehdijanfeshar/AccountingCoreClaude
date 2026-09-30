using Accounting.Application.FinancialStatements.Commands.Common;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Commands.SeedDefaultFsTemplates;

public sealed record FsDefaultTemplate(
    FsFramework Framework,
    string Code,
    string TitleFa,
    string TitleEn,
    FsStatementType StatementType,
    int OrderNo,
    IReadOnlyList<FsTemplateRowInput> Rows,
    string? NoteParentTemplateCode = null,
    string? NoteParentRowCode = null,
    string? NoteTotalRowCode = null);

/// <summary>قاعدهٔ کنترل پیش‌فرض (مشترک، مسدودکننده، اختلاف مجاز صفر).</summary>
public sealed record FsDefaultRule(FsFramework Framework, string Code, string TitleFa, string LeftExpr, string RightExpr);

/// <summary>
/// قالب‌های پیش‌فرض دو مجموعهٔ طرح بیمه‌ای (استاندارد ۲۷) و واحد تجاری (استاندارد ۱) — فاز ۴۵-الف.
///
/// ⚠️ <b>پیش‌نویس برای تأیید کارشناس مالی، نه قالب نهایی.</b> انتخاب‌گرها بر اساس ساختار کدینگ
/// دیده‌شده روی Oracle Development (۲۰۲۶-۰۹-۳۰، فقط SELECT) نوشته شده‌اند: گروه‌های ۲۰/۳۰ دارایی،
/// ۴۰/۵۰ بدهی، ۶۰ درآمد، ۷۰ تعهدات قانونی و درمان، ۸۰ هزینه‌های اداری، ۹۰ انتظامی (در هیچ صورتی
/// نمی‌آید)؛ معین ۶ رقمی با پیشوند کد کل ۴ رقمی. seed همیشه نسخهٔ <b>Draft</b> می‌سازد.
///
/// قرارداد علامت (سند منبع §۷-۳): مقدار داخلی بدهکار-مثبت است؛ ماهیت بستانکار فقط در نمایش قرینه
/// می‌کند. پس «جمع بدهی‌ها» داخلاً منفی است و <c>N99 = A99 + L99</c> خالص دارایی را می‌دهد.
/// </summary>
public static class FsDefaultTemplates
{
    private const FsNormalBalance Dr = FsNormalBalance.Debit;
    private const FsNormalBalance Cr = FsNormalBalance.Credit;

    private static readonly FsRowFormat Subtotal = new(Bold: true, TopBorder: FsBorder.Single);
    private static readonly FsRowFormat GrandTotal = new(Bold: true, TopBorder: FsBorder.Single, BottomBorder: FsBorder.Double);
    private static readonly FsRowFormat Item = new(Indent: 1, InnerColumn: true);

    public static IReadOnlyList<FsDefaultTemplate> All { get; } = new[]
    {
        PensionNetAssets(),
        PensionChangesInNetAssets(),
        PensionEquityMovement(),
        PensionCashFlow(),
        CommercialFinancialPosition(),
        CommercialProfitOrLoss(),
        CommercialCashFlow(),
        PensionNoteCash(),
        PensionNoteReceivables(),
        PensionNotePremium(),
        CommercialNoteCash(),
    };

    /// <summary>
    /// قواعد کنترل پیش‌فرض (بخش ۴۵-ه، سند منبع §۱۰) — همه مسدودکننده. مبالغ داخلی با علامت حسابداری‌اند،
    /// پس «دارایی = بدهی + حقوق مالکانه» به‌صورت <c>A99 + E99 = 0</c> نوشته می‌شود (سمت چپ بستانکار-منفی).
    /// V-03 تا پیش از ورود مقادیر دستی جریان نقد ناموفق می‌ماند — عمداً، تا ورودشان فراموش نشود.
    /// </summary>
    public static IReadOnlyList<FsDefaultRule> Rules { get; } = new[]
    {
        new FsDefaultRule(FsFramework.Pension, "V-03", "موجودی نقد پایان سال در صورت جریان نقدی = موجودی نقد صورت خالص دارایی‌ها",
            "STMT(PENSION.CASH_FLOW, F99)", "STMT(PENSION.NET_ASSETS, A01)"),
        new FsDefaultRule(FsFramework.Pension, "V-04", "خالص دارایی‌های پایان سال در گردش ارزش ویژه = صورت خالص دارایی‌ها",
            "STMT(PENSION.EQUITY_MOVEMENT, Q99)", "STMT(PENSION.NET_ASSETS, N99)"),
        new FsDefaultRule(FsFramework.Commercial, "V-02", "جمع دارایی‌ها = جمع بدهی‌ها و حقوق مالکانه",
            "STMT(COMMERCIAL.FINANCIAL_POSITION, A99) + STMT(COMMERCIAL.FINANCIAL_POSITION, E99)", "0"),
        new FsDefaultRule(FsFramework.Commercial, "V-03", "موجودی نقد پایان سال در صورت جریان نقدی = موجودی نقد صورت وضعیت مالی",
            "STMT(COMMERCIAL.CASH_FLOW, F99)", "STMT(COMMERCIAL.FINANCIAL_POSITION, A54)"),
    };

    // ---- یادداشت‌های نمونه (بخش ۴۵-ج) — «عنوان»ها زیر‌یادداشت‌اند (5-1، 5-2). ----

    private static FsDefaultTemplate PensionNoteCash() => new(
        FsFramework.Pension, "PENSION.NOTE_CASH",
        "موجودی نقد و بانک", "Cash and Bank Balances",
        FsStatementType.Note, 110,
        new[]
        {
            H("S1", "موجودی نزد بانک‌ها"),
            A("C01", "حساب‌های بانکی", "3060*", Dr, parent: "S1"),
            H("S2", "موجودی صندوق و تنخواه‌گردان‌ها"),
            A("C02", "صندوق", "3040*", Dr, parent: "S2"),
            A("C03", "تنخواه‌گردان‌ها", "3050*", Dr, parent: "S2"),
            F("C99", "جمع", "SUM(C01:C03)", Dr, GrandTotal),
        },
        "PENSION.NET_ASSETS", "A01", "C99");

    private static FsDefaultTemplate PensionNoteReceivables() => new(
        FsFramework.Pension, "PENSION.NOTE_RECEIVABLES",
        "حساب‌ها و اسناد دریافتنی", "Receivables",
        FsStatementType.Note, 120,
        new[]
        {
            A("D01", "حساب‌ها و اسناد دریافتنی", "3030*", Dr),
            F("D99", "جمع", "D01", Dr, GrandTotal),
        },
        "PENSION.NET_ASSETS", "A03", "D99");

    private static FsDefaultTemplate PensionNotePremium() => new(
        FsFramework.Pension, "PENSION.NOTE_PREMIUM",
        "درآمد حق بیمه", "Contribution Income",
        FsStatementType.Note, 130,
        new[]
        {
            A("P01", "درآمد حق بیمه", "6010*", Cr, FsValueType.Movement),
            F("P99", "جمع", "P01", Cr, GrandTotal),
        },
        "PENSION.CHANGES_IN_NET_ASSETS", "R01", "P99");

    private static FsDefaultTemplate CommercialNoteCash() => new(
        FsFramework.Commercial, "COMMERCIAL.NOTE_CASH",
        "موجودی نقد", "Cash and Cash Equivalents",
        FsStatementType.Note, 110,
        new[]
        {
            A("C01", "موجودی نزد بانک‌ها", "3060*", Dr),
            A("C02", "صندوق", "3040*", Dr),
            A("C03", "تنخواه‌گردان‌ها", "3050*", Dr),
            F("C99", "جمع", "SUM(C01:C03)", Dr, GrandTotal),
        },
        "COMMERCIAL.FINANCIAL_POSITION", "A54", "C99");

    private static FsDefaultTemplate PensionNetAssets() => new(
        FsFramework.Pension, "PENSION.NET_ASSETS",
        "صورت خالص دارایی‌های در دسترس برای پرداخت مزایا", "Statement of Net Assets Available for Benefits",
        FsStatementType.NetAssets, 10,
        new[]
        {
            H("A", "دارایی‌ها"),
            A("A01", "موجودی نقد و بانک", "3040* 3050* 3060*", Dr, parent: "A"),
            A("A02", "سرمایه‌گذاری‌های کوتاه‌مدت", "3070*", Dr, parent: "A"),
            A("A03", "حساب‌ها و اسناد دریافتنی", "3030*", Dr, parent: "A"),
            A("A04", "سفارشات و پیش‌پرداخت‌ها", "3010*", Dr, parent: "A"),
            A("A05", "سایر دارایی‌های جاری", "3020* 3080*", Dr, parent: "A"),
            A("A06", "دارایی‌های ثابت مشهود", "2010* 2020*", Dr, parent: "A"),
            A("A07", "سایر دارایی‌ها", "2040*", Dr, parent: "A"),
            F("A99", "جمع دارایی‌ها", "SUM(A01:A07)", Dr, Subtotal),
            H("L", "بدهی‌ها"),
            A("L01", "حساب‌ها و اسناد پرداختنی", "4010*", Cr, parent: "L"),
            A("L02", "پیش‌دریافت‌ها", "4020*", Cr, parent: "L"),
            A("L03", "سایر بدهی‌های جاری", "4030* 4040*", Cr, parent: "L"),
            A("L04", "ذخیرهٔ مزایای پایان خدمت کارکنان", "5010*", Cr, parent: "L"),
            F("L99", "جمع بدهی‌ها", "SUM(L01:L04)", Cr, Subtotal),
            F("N99", "خالص دارایی‌های در دسترس برای پرداخت مزایا", "A99 + L99", Dr, GrandTotal),
            B("N00"),
            E("E01", "ارزش فعلی اکچوئری مزایای بازنشستگی تعهدشده", Cr),
        });

    private static FsDefaultTemplate PensionChangesInNetAssets() => new(
        FsFramework.Pension, "PENSION.CHANGES_IN_NET_ASSETS",
        "صورت تغییرات در خالص دارایی‌های در دسترس برای پرداخت مزایا", "Statement of Changes in Net Assets Available for Benefits",
        FsStatementType.ChangesInNetAssets, 20,
        new[]
        {
            H("R", "منابع"),
            A("R01", "درآمد حق بیمه", "6010*", Cr, FsValueType.Movement, "R"),
            A("R02", "درآمد حاصل از سرمایه‌گذاری‌ها", "6020*", Cr, FsValueType.Movement, "R"),
            A("R03", "درآمد حاصل از خسارات و جرایم نقدی", "6030*", Cr, FsValueType.Movement, "R"),
            A("R04", "درآمد حاصل از کمک‌ها و هدایا", "6040*", Cr, FsValueType.Movement, "R"),
            A("R05", "درآمد بیمهٔ بیکاری", "6070*", Cr, FsValueType.Movement, "R"),
            A("R06", "سایر درآمدها", "6050*", Cr, FsValueType.Movement, "R"),
            F("R99", "جمع منابع", "SUM(R01:R06)", Cr, Subtotal),
            H("C", "مصارف"),
            A("C01", "غرامت دستمزد و ایام بیماری", "7010*", Dr, FsValueType.Movement, "C"),
            A("C02", "کمک‌ها", "7020*", Dr, FsValueType.Movement, "C"),
            A("C03", "ازکارافتادگی و غرامت نقص عضو", "7030*", Dr, FsValueType.Movement, "C"),
            A("C04", "بازنشستگی", "7040*", Dr, FsValueType.Movement, "C"),
            A("C05", "بازماندگان", "7050*", Dr, FsValueType.Movement, "C"),
            A("C06", "حق سنوات مستمری‌بگیران", "7060*", Dr, FsValueType.Movement, "C"),
            A("C07", "هزینه‌های بیمهٔ بیکاری", "7070*", Dr, FsValueType.Movement, "C"),
            A("C08", "هزینه‌های درمان مستقیم", "7080*", Dr, FsValueType.Movement, "C"),
            A("C09", "هزینه‌های درمان غیرمستقیم", "7090*", Dr, FsValueType.Movement, "C"),
            A("C10", "حقوق کارکنان", "8010*", Dr, FsValueType.Movement, "C"),
            A("C11", "مزایای کارکنان", "8020*", Dr, FsValueType.Movement, "C"),
            A("C12", "هزینه‌های اداری", "8030* 5101*", Dr, FsValueType.Movement, "C"),
            A("C13", "هزینه‌های کارگزاری", "8040*", Dr, FsValueType.Movement, "C"),
            F("C99", "جمع مصارف", "SUM(C01:C13)", Dr, Subtotal),
            F("X99", "افزایش (کاهش) در خالص دارایی‌های طرح", "R99 + C99", Cr, GrandTotal),
        });

    private static FsDefaultTemplate PensionEquityMovement() => new(
        FsFramework.Pension, "PENSION.EQUITY_MOVEMENT",
        "گردش حساب ارزش ویژه", "Statement of Movement in Equity",
        FsStatementType.EquityMovement, 30,
        new[]
        {
            A("Q01", "خالص دارایی‌ها در ابتدای سال", "20* 30* 40* 50*", Dr, FsValueType.Opening),
            E("Q02", "تعدیلات سنواتی", Dr),
            F("Q03", "خالص دارایی‌ها در ابتدای سال — تعدیل‌شده", "Q01 + Q02", Dr, Subtotal),
            // X99 تغییرات بستانکار-منفی است (افزایش = منفی)؛ خالص دارایی بدهکار-مثبت — پس قرینه.
            F("Q04", "افزایش (کاهش) در خالص دارایی‌ها", "-STMT(PENSION.CHANGES_IN_NET_ASSETS, X99)", Dr),
            F("Q99", "خالص دارایی‌ها در پایان سال", "Q03 + Q04", Dr, GrandTotal),
        });

    private static FsDefaultTemplate PensionCashFlow() => new(
        FsFramework.Pension, "PENSION.CASH_FLOW",
        "صورت جریان‌های نقدی", "Statement of Cash Flows",
        FsStatementType.CashFlow, 40,
        CashFlowRows("3040* 3050* 3060*"));

    private static FsDefaultTemplate CommercialFinancialPosition() => new(
        FsFramework.Commercial, "COMMERCIAL.FINANCIAL_POSITION",
        "صورت وضعیت مالی", "Statement of Financial Position",
        FsStatementType.FinancialPosition, 10,
        new[]
        {
            H("A", "دارایی‌ها"),
            H("AN", "دارایی‌های غیرجاری", "A"),
            A("A01", "دارایی‌های ثابت مشهود", "2010* 2020*", Dr, parent: "AN"),
            A("A02", "سایر دارایی‌های غیرجاری", "2040*", Dr, parent: "AN"),
            F("A49", "جمع دارایی‌های غیرجاری", "SUM(A01:A02)", Dr, Subtotal),
            H("AC", "دارایی‌های جاری", "A"),
            A("A51", "پیش‌پرداخت‌ها", "3010*", Dr, parent: "AC"),
            A("A52", "دریافتنی‌های تجاری و سایر دریافتنی‌ها", "3030*", Dr, parent: "AC"),
            A("A53", "سرمایه‌گذاری‌های کوتاه‌مدت", "3070*", Dr, parent: "AC"),
            A("A54", "موجودی نقد", "3040* 3050* 3060*", Dr, parent: "AC"),
            A("A55", "سایر دارایی‌های جاری", "3020* 3080*", Dr, parent: "AC"),
            F("A98", "جمع دارایی‌های جاری", "SUM(A51:A55)", Dr, Subtotal),
            F("A99", "جمع دارایی‌ها", "A49 + A98", Dr, GrandTotal),
            H("E", "حقوق مالکانه و بدهی‌ها"),
            H("EQ", "حقوق مالکانه", "E"),
            // کدینگ فعلی حساب حقوق مالکانه ندارد — تا نگاشت کدینگ شرکت‌های تابعه (بعد از فاز ۴۵) دستی.
            E("E01", "سرمایه", Cr, "EQ"),
            E("E02", "اندوخته‌ها", Cr, "EQ"),
            E("E03", "سود (زیان) انباشتهٔ ابتدای سال", Cr, "EQ"),
            F("E04", "سود (زیان) خالص سال", "STMT(COMMERCIAL.PROFIT_OR_LOSS, P99)", Cr),
            F("E49", "جمع حقوق مالکانه", "SUM(E01:E04)", Cr, Subtotal),
            H("LN", "بدهی‌های غیرجاری", "E"),
            A("L01", "ذخیرهٔ مزایای پایان خدمت کارکنان", "5010*", Cr, parent: "LN"),
            F("L49", "جمع بدهی‌های غیرجاری", "L01", Cr, Subtotal),
            H("LC", "بدهی‌های جاری", "E"),
            A("L51", "پرداختنی‌های تجاری و سایر پرداختنی‌ها", "4010*", Cr, parent: "LC"),
            A("L52", "پیش‌دریافت‌ها", "4020*", Cr, parent: "LC"),
            A("L53", "سایر بدهی‌های جاری", "4030* 4040*", Cr, parent: "LC"),
            F("L98", "جمع بدهی‌های جاری", "SUM(L51:L53)", Cr, Subtotal),
            F("L99", "جمع بدهی‌ها", "L49 + L98", Cr, Subtotal),
            F("E99", "جمع حقوق مالکانه و بدهی‌ها", "E49 + L99", Cr, GrandTotal),
        });

    private static FsDefaultTemplate CommercialProfitOrLoss() => new(
        FsFramework.Commercial, "COMMERCIAL.PROFIT_OR_LOSS",
        "صورت سود و زیان", "Statement of Profit or Loss",
        FsStatementType.ProfitOrLoss, 20,
        new[]
        {
            A("P01", "درآمدهای عملیاتی", "6010* 6070*", Cr, FsValueType.Movement),
            A("P02", "بهای تمام‌شدهٔ درآمدهای عملیاتی", "70*", Dr, FsValueType.Movement),
            F("P03", "سود (زیان) ناخالص", "P01 + P02", Cr, Subtotal),
            A("P04", "هزینه‌های فروش، اداری و عمومی", "80* 5101*", Dr, FsValueType.Movement),
            F("P05", "سود (زیان) عملیاتی", "P03 + P04", Cr, Subtotal),
            A("P06", "درآمد سرمایه‌گذاری‌ها", "6020*", Cr, FsValueType.Movement),
            A("P07", "سایر درآمدها و هزینه‌های غیرعملیاتی", "6030* 6040* 6050*", Cr, FsValueType.Movement),
            F("P99", "سود (زیان) خالص", "P05 + P06 + P07", Cr, GrandTotal),
        });

    private static FsDefaultTemplate CommercialCashFlow() => new(
        FsFramework.Commercial, "COMMERCIAL.CASH_FLOW",
        "صورت جریان‌های نقدی", "Statement of Cash Flows",
        FsStatementType.CashFlow, 30,
        CashFlowRows("3040* 3050* 3060*"));

    /// <summary>
    /// صورت جریان نقدی: گردش‌های طبقه‌بندی‌شده از مانده‌ها درنمی‌آیند (<c>docs/fs-module.md</c> §۵)،
    /// پس سه جریان اصلی فعلاً ردیف دستی‌اند؛ مانده ابتدا از حساب‌ها و مانده پایان محاسبه‌ای است.
    /// </summary>
    private static FsTemplateRowInput[] CashFlowRows(string cashSelector) => new[]
    {
        E("F01", "جریان خالص ورود (خروج) نقد حاصل از فعالیت‌های عملیاتی", Dr),
        E("F02", "جریان خالص ورود (خروج) نقد حاصل از فعالیت‌های سرمایه‌گذاری", Dr),
        E("F03", "جریان خالص ورود (خروج) نقد حاصل از فعالیت‌های تأمین مالی", Dr),
        F("F04", "خالص افزایش (کاهش) در موجودی نقد", "SUM(F01:F03)", Dr, Subtotal),
        A("F05", "موجودی نقد در ابتدای سال", cashSelector, Dr, FsValueType.Opening),
        E("F06", "تأثیر تغییرات نرخ ارز", Dr),
        F("F99", "موجودی نقد در پایان سال", "F04 + F05 + F06", Dr, GrandTotal),
    };

    private static FsTemplateRowInput H(string code, string title, string? parent = null)
        => new(code, FsRowType.Header, title, ParentCode: parent, Format: new FsRowFormat(Bold: true), IsDrillable: false);

    private static FsTemplateRowInput A(
        string code, string title, string selector, FsNormalBalance nb,
        FsValueType valueType = FsValueType.Closing, string? parent = null)
        => new(code, FsRowType.Account, title, ParentCode: parent, NormalBalance: nb, Selector: selector,
            ValueType: valueType, Format: Item);

    private static FsTemplateRowInput F(string code, string title, string formula, FsNormalBalance nb, FsRowFormat? format = null)
        => new(code, FsRowType.Formula, title, NormalBalance: nb, Formula: formula, Format: format);

    private static FsTemplateRowInput E(string code, string title, FsNormalBalance nb, string? parent = null)
        => new(code, FsRowType.External, title, ParentCode: parent, NormalBalance: nb, Format: Item, IsDrillable: false, AllowManualAdjust: true);

    private static FsTemplateRowInput B(string code)
        => new(code, FsRowType.Blank, null, IsDrillable: false);
}
