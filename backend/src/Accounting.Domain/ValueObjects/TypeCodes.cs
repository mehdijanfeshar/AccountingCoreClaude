namespace Accounting.Domain.ValueObjects;

/// <summary>
/// سطح سلسله‌مراتب کدینگ حساب (گروه/کل/معین) — نگاشت مقدار عددی ستون
/// <c>TB_ACCOUNTCODE.TYPECODE</c> (فعلاً به‌اشتباه <c>bool?</c>، رجوع به ریسک باز #۲ در
/// <c>CLAUDE.md</c>) به این enum.
///
/// معادل enum <c>TypeCodes</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeCodes..cs</c> — نام فایل واقعاً
/// دو نقطه دارد، تایپوی خودشان): <c>Group = 1</c>, <c>Kol = 2</c>, <c>Moin = 3</c>. کوئری‌های
/// EF پروژهٔ مرجع (مثلاً <c>a.TypeCode == TypeCodes.Kol</c>) بدون هیچ ValueConverter به
/// <c>TYPECODE = 2</c> در SQL ترجمه می‌شوند — پس مقدار فیزیکی در دیتابیس قطعاً ۱/۲/۳ است
/// (رجوع به <c>docs/centralaccount-business-reference.md</c> §۱-۱).
///
/// ⚠️ نام‌گذاری عمداً جمع (<c>TypeCodes</c>) است، نه <c>TypeCode</c> مفرد، به دو دلیل:
/// (الف) <c>System.TypeCode</c> از قبل در BCL وجود دارد و به‌واسطهٔ implicit usings در scope
/// هر فایل مصرف‌کننده هست؛ نام مفرد باعث ابهام CS0104 می‌شد.
/// (ب) خودِ enum پروژهٔ مرجع هم دقیقاً همین نام جمع (<c>TypeCodes</c>) را دارد — پس این نام هم
/// از تناقض BCL جلوگیری می‌کند و هم به منبع مرجع وفادارتر است.
/// </summary>
public enum TypeCodes
{
    /// <summary>گروه — سطح اول کدینگ.</summary>
    Group = 1,

    /// <summary>کل — سطح دوم کدینگ.</summary>
    Kol = 2,

    /// <summary>معین — سطح سوم کدینگ.</summary>
    Moin = 3,
}
