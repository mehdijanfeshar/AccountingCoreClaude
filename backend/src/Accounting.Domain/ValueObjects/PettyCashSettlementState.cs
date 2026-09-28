namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت یک دورهٔ تسویهٔ تنخواه (<c>TB_PC_SETTLEMENT_PERIOD.STATE</c>) — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> §۹، ۲۰۲۶-۰۹-۲۸).
///
/// دو مقدار ساده، برخلاف چرخهٔ حالت‌های چندمرحله‌ایِ ترمیم/صورت‌هزینه: یک دوره یا هنوز شمارش‌شدهٔ
/// بازبینی‌نشده است (<see cref="Draft"/>) یا با سند GL نهایی شده (<see cref="Final"/>) — پس از
/// نهایی‌سازی دیگر قابل تغییر نیست (نه ویرایش، نه حذف؛ رجوع به <c>FinalizePettyCashSettlementCommandHandler</c>).
/// </summary>
public enum PettyCashSettlementState
{
    /// <summary>پیش‌نویس — شمارش صندوق (<c>COUNTED_BALANCE</c>) ممکن است ثبت شده یا نشده باشد،
    /// هنوز سند GL صادر نشده. تنها وضعیت قابل شمارش/ویرایش شمارش.</summary>
    Draft = 1,

    /// <summary>نهایی — سند GL صادر و <c>VOUCHERSHEAD_ID</c> ثبت شده؛ صورت‌هزینه‌های منظورشده
    /// <see cref="PettyCashDocState.Settled"/> شدند. پایانی، بدون بازگشت.</summary>
    Final = 2,
}
