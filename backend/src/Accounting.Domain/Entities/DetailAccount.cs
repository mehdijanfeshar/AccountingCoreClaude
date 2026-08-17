using Accounting.Domain.Common;
using Accounting.Domain.Exceptions;

namespace Accounting.Domain.Entities;

/// <summary>
/// مقدار واقعیِ تفصیلی (مثلاً "شرکت پارس" زیر نوع "مشتریان"). عمداً هیچ ارجاعی به
/// SubsidiaryAccount در این موجودیت وجود ندارد: رابطهٔ معین با تفصیلی فقط در سطح «نوع»
/// (از طریق SubsidiaryDetailTypeLink) برقرار می‌شود، نه در سطح «مقدار». این دقیقاً همان
/// چیزی است که قانون ۴ می‌خواهد: در تعریف معین هیچ DetailAccount مشخصی ثبت نمی‌شود؛
/// این‌جا با نبودِ چنین فیلدی به‌صورت ساختاری تضمین شده، نه صرفاً با قرارداد.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class DetailAccount
{
    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;

    public int DetailAccountTypeId { get; private set; }
    public DetailAccountType DetailAccountType { get; private set; } = null!;

    // برای EF Core
    private DetailAccount()
    {
    }

    private DetailAccount(DetailAccountType detailAccountType, string code, string title)
    {
        DetailAccountType = detailAccountType;
        DetailAccountTypeId = detailAccountType.Id;
        Code = ValidateCode(code);
        Title = Guard.Title(title);
    }

    public static DetailAccount Create(DetailAccountType detailAccountType, string code, string title)
        => new(detailAccountType, code, title);

    private static string ValidateCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidAccountCodeException("کد حساب تفصیلی نمی‌تواند خالی باشد.");

        return code.Trim();
    }
}
