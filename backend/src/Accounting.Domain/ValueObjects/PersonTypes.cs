namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع شخص (حقیقی/حقوقی/سایر) — نگاشت مقدار عددی ستون <c>PERSONTYPE</c> روی هر دو جدول
/// <c>TB_TAFSILI</c> و <c>TB_TAFSIL_GROUP</c> (فعلاً به‌اشتباه <c>bool?</c> در هر دو، رجوع به
/// ریسک باز #۲ در <c>CLAUDE.md</c> و <c>docs/centralaccount-business-reference.md</c> §۲۴-۱) به
/// این یک enum مشترک.
///
/// معادل enum <c>PersonTypes</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Tafsiliies\PersonTypes.cs</c>): مقادیر و نام‌گذاری
/// دقیقاً همان‌جا نیز جمع (<c>PersonTypes</c>) است، بدون نیاز به تغییر نام برای رفع تداخل با BCL
/// (برخلاف <see cref="TypeCodes"/>).
/// </summary>
public enum PersonTypes
{
    /// <summary>حقیقی.</summary>
    Person = 1,

    /// <summary>حقوقی.</summary>
    Legal = 2,

    /// <summary>سایر.</summary>
    Other = 3,
}
