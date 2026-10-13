namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// قاعدهٔ چک نقض شد (چک ابطال‌شده یا استفاده‌شده در ردیف دیگر، گذار نامجاز کارتابل، چاپ چک تأییدنشده…)
/// — ۴۰۹ با پیام فارسی.
/// </summary>
public sealed class ChequeConflictException : Exception, IPublicProblemException
{
    public ChequeConflictException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public int StatusCode => 409;

    public string PublicDetail { get; }
}
