namespace Accounting.Domain.ValueObjects;

/// <summary>
/// موقت/دائم‌بودن حساب — نگاشت مقدار عددی ستون <c>TB_ACCOUNTCODE.TYPEACCCODE</c> (فعلاً
/// به‌اشتباه <c>bool?</c>، رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>) به این enum. فقط روی سطح
/// گروه ست می‌شود (رجوع به <c>docs/centralaccount-business-reference.md</c> §۱-۲).
///
/// معادل enum <c>TypeAccCode</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeAccCode.cs</c>): اعضای آن‌جا با
/// حروف کوچک (<c>temporary = 1</c>, <c>permanent = 2</c>) نام‌گذاری شده‌اند؛ اینجا طبق قرارداد
/// نام‌گذاری .NET از PascalCase استفاده شده — مقادیر عددی کاملاً یکسان‌اند.
///
/// این تنها یکی از این چهار ستون است که کامنت موجود ستون Oracle («نوع حساب
/// (1موقت2دائم)») **درست** است و با enum هم‌راستاست (برخلاف <see cref="TypeActivity"/> که
/// کامنتش نادرست/کهنه است).
/// </summary>
public enum TypeAccCode
{
    /// <summary>موقت — معادل <c>temporary = 1</c> در پروژهٔ مرجع.</summary>
    Temporary = 1,

    /// <summary>دائم — معادل <c>permanent = 2</c> در پروژهٔ مرجع.</summary>
    Permanent = 2,
}
