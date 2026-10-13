namespace Accounting.Application.Common.Exceptions;

/// <summary>خطای برگشت صورتحساب ماه (رمز نادرست/نبودِ رمز/نبودِ سند/تنظیمات). 409.</summary>
public sealed class MonthReopenException : Exception, IPublicProblemException
{
    public MonthReopenException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public int StatusCode => 409;

    public string PublicDetail { get; }
}
