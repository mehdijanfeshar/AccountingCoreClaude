using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

/// <summary>
/// «نوع» تفصیلی شناور (مثلاً مشتریان، پروژه‌ها، مراکز هزینه). این موجودیت داده‌ای مرجع
/// و مستقل از هر معین خاصی است؛ اتصال آن به معین‌ها فقط از طریق
/// <see cref="SubsidiaryDetailTypeLink"/> (که در سمت <see cref="SubsidiaryAccount"/> مدیریت می‌شود)
/// برقرار می‌شود، نه از این سمت.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class DetailAccountType
{
    public int Id { get; private set; }
    public string Title { get; private set; } = null!;

    // برای EF Core
    private DetailAccountType()
    {
    }

    private DetailAccountType(string title)
    {
        Title = Guard.Title(title);
    }

    public static DetailAccountType Create(string title) => new(title);

    public void Rename(string title) => Title = Guard.Title(title);
}
