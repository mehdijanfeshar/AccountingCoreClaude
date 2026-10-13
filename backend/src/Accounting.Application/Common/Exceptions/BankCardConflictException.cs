namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// قاعدهٔ «کارت حساب جاری» نقض شد (ویرایش/حذف ردیف مغایرت‌گیری‌شده، ماه دیسکت، …) — ۴۰۹ با پیام فارسی.
/// </summary>
public sealed class BankCardConflictException : Exception, IPublicProblemException
{
    public BankCardConflictException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public int StatusCode => 409;

    public string PublicDetail { get; }
}
