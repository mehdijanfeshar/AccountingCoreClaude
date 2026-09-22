namespace Accounting.Domain.ValueObjects;

/// <summary>
/// فعال/غیرفعال بودن یک ردیف تفصیلی — نگاشت مقدار عددی ستون <c>TB_TAFSILI.ISACTIVE</c> (فعلاً
/// به‌اشتباه <c>bool?</c>، رجوع به ریسک باز #۲ در <c>CLAUDE.md</c> و
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱) به این enum.
///
/// معادل enum <c>Active</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Tafsiliies\Active.cs</c>): <c>IsActive = 1</c>,
/// <c>DeActive = 2</c>.
///
/// ⚠️ نام‌گذاری عمداً <c>TafsiliActiveState</c> است، نه <c>Active</c> مثل پروژهٔ مرجع: نام مفرد
/// <c>Active</c> در فضای نام مشترک <c>ValueObjects</c> (که انواع بی‌ربط زیادی در آن زندگی
/// می‌کنند) به‌طرز گمراه‌کننده‌ای کلی است و در فراخوانی/IntelliSense معنای خودش را نمی‌رساند؛
/// این enum دقیقاً و فقط برای <c>TB_TAFSILI.ISACTIVE</c> است، برای همین به نام آن ستون/جدول
/// گره خورده است.
///
/// **مهم:** <c>۰</c> عضو این enum نیست — بر خلاف فرض رایج «اگر دومقداری باشد <c>bool</c>
/// بی‌خطر است»، این enum از ۱ شروع می‌شود. نگاشت <c>false → 0</c> در دادهٔ Legacy معنایی ندارد؛
/// تنها دو مقدار معتبر <c>۱</c> (فعال) و <c>۲</c> (غیرفعال) هستند.
/// </summary>
public enum TafsiliActiveState
{
    /// <summary>فعال — معادل <c>IsActive = 1</c> در پروژهٔ مرجع.</summary>
    IsActive = 1,

    /// <summary>غیرفعال — معادل <c>DeActive = 2</c> در پروژهٔ مرجع.</summary>
    DeActive = 2,
}
