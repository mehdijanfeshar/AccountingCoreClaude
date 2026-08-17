namespace Accounting.Domain.Entities;

/// <summary>
/// مقدار واقعیِ یک تفصیلی که در زمان صدور سند برای یک ردیف مشخص ثبت شده است (قانون ۴).
/// عمداً فقط شناسه‌ها (DetailAccountTypeId / DetailAccountId) نگه‌داری می‌شود، نه ارجاع زنده
/// به DetailAccount (که در Aggregate جداگانه‌ای زندگی می‌کند)؛ این خودِ VoucherLine است که
/// در لحظهٔ ساخت، تعلق DetailAccount به نوع درست را تضمین می‌کند (رجوع به VoucherLine).
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class VoucherLineDetailValue
{
    public int Id { get; private set; }

    public int VoucherLineId { get; private set; }
    public VoucherLine VoucherLine { get; private set; } = null!;

    public int DetailAccountTypeId { get; private set; }
    public int DetailAccountId { get; private set; }

    // برای EF Core
    private VoucherLineDetailValue()
    {
    }

    private VoucherLineDetailValue(VoucherLine voucherLine, int detailAccountTypeId, int detailAccountId)
    {
        VoucherLine = voucherLine;
        VoucherLineId = voucherLine.Id;
        DetailAccountTypeId = detailAccountTypeId;
        DetailAccountId = detailAccountId;
    }

    internal static VoucherLineDetailValue Create(VoucherLine voucherLine, int detailAccountTypeId, int detailAccountId)
        => new(voucherLine, detailAccountTypeId, detailAccountId);
}
