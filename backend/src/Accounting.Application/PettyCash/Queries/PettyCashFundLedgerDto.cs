namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/ledger?from=&amp;to=&amp;type=</c> — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۱۱ پاورپوینت).
///
/// <b>⚠️ این گزارش تاریخچهٔ حرکت واقعی وجه است، نه معادل لحظه‌ایِ فرمول موجودی نقد.</b> فقط
/// صورت‌هزینه‌های <c>Approved</c>/<c>Settled</c> ردیف «پرداخت» می‌سازند — اسناد
/// <c>New</c>/<c>PendingReview</c>/<c>Returned</c> (که فرمول §۲/۳-الف آن‌ها را هم به‌عنوان «در
/// جریان» کم می‌کند) اینجا ظاهر نمی‌شوند، چون هنوز قطعی نشده‌اند. پس <see cref="ClosingBalance"/>
/// وقتی <c>to</c> امروز باشد با <c>cashBalance</c> فعلی تنخواه (از داشبورد/فهرست تنخواه‌ها) برابر
/// <b>نیست</b> — اختلافشان دقیقاً مجموع اسناد در جریان است. این تفاوت عمدی است، نه باگ.
///
/// <b>موجودی اولیه:</b> <c>TB_PC_FUND.CEILING</c> همیشه به‌عنوان «تخصیص سقف اولیه» پیش از هر
/// بازهٔ درخواستی لحاظ می‌شود (تصمیم ۲۰۲۶-۰۹-۲۸ — پیشنهاد سند طراحی)؛ به همراه هر ترمیم
/// <c>Paid</c>/استرداد/صورت‌هزینهٔ تأییدشده با تاریخ قبل از <c>from</c>.
/// </summary>
public sealed record PettyCashFundLedgerDto(
    decimal OpeningBalance,
    IReadOnlyList<PettyCashLedgerRowDto> Rows,
    decimal TotalReceipt,
    int ReceiptCount,
    decimal TotalPayment,
    int PaymentCount,
    decimal ClosingBalance);

/// <param name="Date">شمسی YYYYMMDD.</param>
/// <param name="Type"><c>"replenishment"</c> | <c>"expense"</c> | <c>"refund"</c>.</param>
/// <param name="Reference">کد نمایشی (<c>RCH-</c>/<c>TH-</c>/<c>REF-</c> + شماره).</param>
/// <param name="Receipt">مبلغ دریافت — فقط برای <c>replenishment</c>/<c>refund</c>؛ در غیر این صورت <see langword="null"/>.</param>
/// <param name="Payment">مبلغ پرداخت — فقط برای <c>expense</c>؛ در غیر این صورت <see langword="null"/>.</param>
/// <param name="Balance">مانده پس از این ردیف — محاسبه‌شده روی <b>کل</b> تاریخچه (فارغ از فیلتر
/// <c>type</c>)، پس فیلترکردن نوع، مانده‌های نمایش‌داده‌شده را گمراه‌کننده نمی‌کند.</param>
/// <param name="SourceId">شناسهٔ موجودیت مبدأ (ترمیم/صورت‌هزینه/استرداد).</param>
public sealed record PettyCashLedgerRowDto(
    string Date,
    string Type,
    string Reference,
    string? Description,
    decimal? Receipt,
    decimal? Payment,
    decimal Balance,
    Guid SourceId);
