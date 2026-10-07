namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// ماتریس دسترسی کدینگ (ریسک #۲۷، تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷) روی ثبت <b>دستی</b> آرتیکل:
/// معین برای نوع واحدِ سند (<c>TB_VAHED_INFO.VAHEDTYPE_ID</c>) در <c>TB_WHITEANDBLACKLIST</c>
/// <list type="bullet">
/// <item>«سیاه» ⇒ ممنوع.</item>
/// <item>«فقط سیستمی» که تاریخ سند در بازهٔ محدودیتش است ⇒ ثبت دستی ممنوع (فقط از «دریافت اسناد سایر سیستم‌ها»).</item>
/// <item>«مجاز» که تاریخ سند در بازهٔ مجازش است ⇒ آزاد.</item>
/// <item>هیچ ردیف مجاز ⇒ <b>ممنوع</b> (پیش‌فرض ممنوع).</item>
/// </list>
/// ردیف بدون نوع واحد برای همهٔ نوع‌ها است. بازهٔ خالی = بی‌انتها. سندهایی که ماژول‌ها خودشان
/// می‌سازند (خزانه، اعلامیه، تنخواه، دریافت از سایر سیستم‌ها) این کنترل را ندارند.
/// </summary>
public interface IAccountEntryPolicy
{
    /// <exception cref="Accounting.Application.Common.Exceptions.BusinessRuleException">اولین معین ممنوع، با کد و دلیل.</exception>
    Task EnsureManualEntryAllowedAsync(
        string vahedCode,
        string? dateDoc,
        IEnumerable<Guid?> accountIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// سند دریافتی از سایر سیستم‌ها: «فقط سیستمی» و «مجاز» هر دو قبول؛ «سیاه» و «بی‌ردیف» رد.
    /// </summary>
    Task EnsureSystemEntryAllowedAsync(
        string vahedCode,
        string? dateDoc,
        IEnumerable<Guid?> accountIds,
        CancellationToken cancellationToken = default);
}
