using Accounting.Application.FinancialStatements.Engine;
using Accounting.Application.Queries.FinancialStatements;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// ماندهٔ معین‌ها برای موتور صورت‌های مالی — فاز ۴۵-ب (<c>docs/fs-module.md</c> §۷). مستقیم از
/// <c>TB_VOUCHERSDETAIL</c>/<c>TB_VOUCHERSHEAD</c> (تصمیم صاحب پروژه؛ جدول مانده نداریم) با یک کوئری
/// تجمیعی به‌ازای هر ستون.
/// </summary>
public interface IFsBalanceReadRepository
{
    /// <summary>
    /// مانده به‌ازای هر (معین، واحد سند) — <see cref="FsAccountBalance.VahedCode"/> پُر است؛ جمع واحدها با
    /// فراخوان. فقط معین‌هایی که حداقل یک ردیف سند دارند. فقط اسناد سال <paramref name="year"/>، واحدهای
    /// <paramref name="vahedCodes"/>، <c>DOCLIFE &gt;= minDocLife</c>، حذف‌نشده، و <b>بدون سند اختتامیه</b>
    /// (<c>FLAG_STATE = 1</c>). «ابتدا» = سند افتتاحیه (سندی با ردیفی روی حساب رابط افتتاحیه،
    /// <c>TB_ACCOUNTCODE_INTERFACE.TYPE = 1</c>) + اسناد پیش از <paramref name="fromDate"/>؛ «دوره» =
    /// بقیهٔ اسناد تا <paramref name="toDate"/> (شامل).
    /// </summary>
    /// <summary>
    /// بخش ۴۵-د، سطح «سند» Drill-down: ردیف‌های سند معین <paramref name="accCode"/> با همان فیلترهای
    /// <see cref="GetBalancesAsync"/>، محدود به <paramref name="window"/>، به ترتیب تاریخ، صفحه‌بندی‌شده.
    /// </summary>
    Task<FsDrillVoucherPageDto> GetVoucherLinesAsync(
        string year,
        string fromDate,
        string toDate,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        string accCode,
        FsDrillWindow window,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// کنترل V-09 (بخش ۴۵-ه): اسناد سال <paramref name="year"/> در همان دامنه و فیلترهای اجرا (جز سند افتتاحیه)
    /// که تاریخشان خالی است یا مال سال دیگری است. <see cref="GetBalancesAsync"/> اولی‌ها را کنار می‌گذارد و
    /// دومی‌ها را بسته به تاریخ در «ابتدا» یا بیرون از دوره می‌گذارد — بی‌صدا؛ این کنترل آشکارشان می‌کند.
    /// </summary>
    Task<FsOutOfPeriodVouchers> GetOutOfPeriodVouchersAsync(
        string year,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        CancellationToken cancellationToken = default);

    /// <summary>بخش ۴۵-و — همهٔ معین‌های حذف‌نشدهٔ کدینگ با کل و گروه والد، به ترتیب کد (نمای «نگاشت حساب‌ها»).</summary>
    Task<IReadOnlyList<Queries.FinancialStatements.AccountMapping.FsChartMoein>> GetChartMoeinsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FsAccountBalance>> GetBalancesAsync(
        string year,
        string fromDate,
        string toDate,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ط-۶/ط-۷ — همهٔ ردیف‌های اسناد دورهٔ (بدون افتتاحیه) با همان فیلترهای <see cref="GetBalancesAsync"/> که حداقل
    /// یک ردیف روی <paramref name="touchAccCodes"/> دارند، جمع‌شده به تفکیک (معین، واحد). برای جریان نقد (حساب‌های
    /// نقد) و تجدید ارائه (حساب تعدیلات سنواتی).
    /// </summary>
    Task<IReadOnlyList<FsTouchLine>> GetTouchingVoucherLinesAsync(
        string year,
        string fromDate,
        string toDate,
        IReadOnlyCollection<string> vahedCodes,
        int minDocLife,
        IReadOnlyCollection<string> touchAccCodes,
        CancellationToken cancellationToken = default);

    /// <summary>کد همهٔ معین‌های زنده — برای اعمال انتخاب‌گر قواعد حذف فی‌مابین.</summary>
    Task<IReadOnlyList<string>> GetMoeinCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// تطبیق فی‌مابین به تفکیک جفت واحد: مانده به‌ازای (واحد سند، معین، واحد طرف مقابل) با همان فیلترهای
    /// <see cref="GetBalancesAsync"/> (تا پایان دوره، با افتتاحیه، بدون اختتامیه). طرف مقابل (تصمیم صاحب پروژه
    /// ۲۰۲۶-۱۰-۱۵): ۴ رقم اول شناسهٔ ردیف سند اگر کد واحد باشد، وگرنه واحد فرستنده/گیرندهٔ اعلامیهٔ همان سند؛
    /// نبود هر دو = null.
    /// </summary>
    Task<IReadOnlyList<FsCounterpartyBalance>> GetCounterpartyBalancesAsync(
        string year, string fromDate, string toDate, IReadOnlyCollection<string> vahedCodes, int minDocLife,
        IReadOnlyCollection<string> accCodes, CancellationToken cancellationToken = default);

    /// <summary>ردیف‌های سند واحد <paramref name="owner"/> با طرف مقابل <paramref name="counterparty"/> (null = نامعلوم)؛ حداکثر ۱۰۰۰.</summary>
    Task<IReadOnlyList<FsCounterpartyLine>> GetCounterpartyLinesAsync(
        string year, string toDate, int minDocLife, IReadOnlyCollection<string> accCodes, string owner, string? counterparty,
        CancellationToken cancellationToken = default);
}

public sealed record FsTouchLine(string AccCode, string? AccName, string? VahedCode, decimal Debtor, decimal Creditor);

/// <param name="Counterparty">کد واحد طرف مقابل؛ null = از شناسه و اعلامیه معلوم نشد.</param>
public sealed record FsCounterpartyBalance(string VahedCode, string AccCode, string? Counterparty, decimal Debtor, decimal Creditor);

/// <param name="Source"><c>A</c> = از شناسهٔ ردیف، <c>E</c> = از اعلامیه، null = نامعلوم.</param>
public sealed record FsCounterpartyLine(
    Guid VoucherHeadId, string? DocNum, string? DateDoc, string VahedCode, string AccCode, string? Counterparty, string? Source,
    string? AttributeValue, string? LineDesc, decimal Debtor, decimal Creditor);
