namespace Accounting.Domain.ValueObjects;

/// <summary>
/// مالکیت ردیف تفصیلی (سراسری/داخلی) — نگاشت مقدار عددی ستون <c>TB_TAFSILI.OWNER</c> (فعلاً
/// به‌اشتباه <c>bool?</c>، رجوع به ریسک باز #۲ در <c>CLAUDE.md</c> و
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱) به این enum.
///
/// معادل enum <c>Owners</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\Tafsiliies\Owners.cs</c>): <c>Global = 1</c>
/// (توضیح <c>[Description("سراسری")]</c>)، <c>Unit = 2</c> (توضیح <c>[Description("داخلی")]</c>) —
/// تأییدشده با خواندن مستقیم فایل مرجع، نه حدس.
///
/// ⚠️ **کامنت قدیمی روی خودِ ستون Oracle («2=setad 1=vahed») با این enum ناهم‌راستاست، دقیقاً به
/// همان شکلی که کامنت ستون <c>TB_ACCOUNTCODE.TYPEACTIVITY</c> نادرست/کهنه بود (رجوع
/// <see cref="TypeActivity"/>).** کامنت Oracle می‌گوید ۲=ستاد (سراسری) و ۱=واحد (داخلی) —
/// دقیقاً برعکسِ enum پروژهٔ مرجع که ۱=<see cref="Global"/> (سراسری) و ۲=<see cref="Unit"/>
/// (داخلی) است.
///
/// این تناقض در ۲۰۲۶-۰۹-۱۵ **با تأیید صریح صاحب پروژه** حل شد: مقادیر زیر (۱=سراسری، ۲=داخلی)
/// درست‌اند و کامنت ستون Oracle کهنه است. هیچ ویرایشگر آینده نباید این مقادیر را بر اساس کامنت
/// ستون Oracle «تصحیح» کند.
/// </summary>
public enum Owners
{
    /// <summary>سراسری — معادل <c>Global = 1</c> در پروژهٔ مرجع (کامنت ستون Oracle این را به‌اشتباه ۲ گزارش می‌کند).</summary>
    Global = 1,

    /// <summary>داخلی (واحد) — معادل <c>Unit = 2</c> در پروژهٔ مرجع (کامنت ستون Oracle این را به‌اشتباه ۱ گزارش می‌کند).</summary>
    Unit = 2,
}
