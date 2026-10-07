namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// قاعدهٔ سطوح تفصیلی (ریسک #۲۳، ۲۰۲۶-۱۰-۰۷): کد سطح میان سطوح فعال یکتاست و حداکثر ۷ سطح
/// فعال مجاز است (مسیر صدور سند مرجع فقط Tafsili1..Tafsili7 را نگاشت می‌کند). 409.
/// </summary>
public sealed class LevelTafsilRuleException : Exception
{
    public LevelTafsilRuleException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
