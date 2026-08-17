using Accounting.Domain.Entities;
using Accounting.Domain.Exceptions;

namespace Accounting.Domain.Rules;

/// <summary>
/// سرویس دامنهٔ بی‌حالت (Stateless Domain Service) که پیش از Post شدن یک سند، آن را
/// به‌طور کامل بازبینی می‌کند: قانون کسب‌وکار اصلی پروژه (رجوع به CLAUDE.md و
/// chart-of-accounts.md) — یعنی الزامی بودن مقادیر تفصیلی طبق SubsidiaryDetailTypeLink —
/// دقیقاً همین‌جا (به همراه VoucherLine.AttachDetailValues در لحظهٔ ساخت ردیف) enforce می‌شود.
///
/// چرا این کلاس لازم است با این‌که VoucherLine خودش هم در لحظهٔ ساخت اعتبارسنجی می‌کند:
/// EF Core هنگام بازیابی موجودیت‌ها از دیتابیس، سازندهٔ خصوصی/فکتوری دامنه را دور می‌زند و
/// مستقیماً فیلدها را از طریق reflection پر می‌کند. بنابراین یک Voucher که از DB لود شده
/// ممکن است بدون عبور از VoucherLine.Create به وضعیت حافظه رسیده باشد. برای همین، این
/// Validator به‌عنوان «آخرین خط دفاعی» درست پیش از تغییر وضعیت به Posted، صرفاً با داده‌های
/// موجود روی خودِ Voucher/VoucherLine (بدون هیچ دسترسی به دیتابیس) دوباره همه‌چیز را چک می‌کند.
///
/// <para>
/// <b>منسوخ (۲۰۲۶-۰۸-۱۷):</b> طبق تصمیم آگاهانهٔ کاربر، مدل نوشتن معتبر پروژه اکنون
/// Entityهای <c>Accounting.Domain.Legacy</c> (schema واقعی Oracle) هستند، نه این مدل Rich.
/// این کلاس صرفاً برای مستندسازی اجرایی (تست‌های موجود) نگه داشته شده و نباید در کد جدید
/// استفاده شود.
/// </para>
/// </summary>
[Obsolete("طبق تصمیم کاربر در ۲۰۲۶-۰۸-۱۷، مدل نوشتن معتبر پروژه Entityهای Accounting.Domain.Legacy است؛ این تایپ منسوخ است و برای کد جدید استفاده نشود.")]
public sealed class VoucherPostingValidator
{
    public void Validate(Voucher voucher)
    {
        if (voucher.Lines.Count == 0)
            throw new EmptyVoucherException(voucher.Number);

        foreach (var line in voucher.Lines)
            line.ReValidateForPosting();

        if (!voucher.IsBalanced)
            throw new VoucherNotBalancedException(voucher.Number, voucher.TotalDebit.Amount, voucher.TotalCredit.Amount);
    }
}
