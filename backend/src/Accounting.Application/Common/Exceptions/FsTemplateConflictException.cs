namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// قاعدهٔ وضعیت/یکتایی قالب صورت مالی نقض شد (مثلاً ویرایش نسخهٔ غیرپیش‌نویس، دو پیش‌نویس هم‌زمان،
/// کد تکراری) — ۴۰۹ با پیام فارسی قابل نمایش. فاز ۴۵-الف.
/// </summary>
public sealed class FsTemplateConflictException : Exception
{
    public FsTemplateConflictException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
