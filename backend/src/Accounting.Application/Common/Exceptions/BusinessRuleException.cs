namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// نقض یک قاعدهٔ کسب‌وکار که کاربر خودش می‌تواند رفعش کند (۴۰۹). متن فارسی عمومی در
/// <see cref="PublicDetail"/>؛ برای قاعده‌های تازه به‌جای ساختن یک Exception جدا برای هر کدام.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
