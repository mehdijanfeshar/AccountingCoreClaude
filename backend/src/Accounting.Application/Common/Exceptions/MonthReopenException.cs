namespace Accounting.Application.Common.Exceptions;

/// <summary>خطای برگشت صورتحساب ماه (رمز نادرست/نبودِ رمز/نبودِ سند/تنظیمات). 409.</summary>
public sealed class MonthReopenException : Exception
{
    public MonthReopenException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public string PublicDetail { get; }
}
