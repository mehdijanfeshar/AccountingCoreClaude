namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// قاعدهٔ گردش اعلامیه نقض شد (ویرایش اعلامیهٔ دارای سند، تأیید بدون سند، حساب رابط تعریف‌نشده و…)
/// — ۴۰۹ با پیام فارسی قابل نمایش.
/// </summary>
public sealed class ElamConflictException : Exception
{
    public ElamConflictException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
