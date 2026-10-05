namespace Accounting.Application.Common.Exceptions;

/// <summary>کاربر نقش لازم برای این کار را ندارد (یا نقش مدیریتی فقط‌مشاهده است) — ۴۰۳ با پیام فارسی.</summary>
public sealed class RoleAccessDeniedException : Exception
{
    public RoleAccessDeniedException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
