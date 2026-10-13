namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// فایل دیسکت بانک با قالب نمی‌خواند (نام فایل، طول ردیف، شمارهٔ حساب، بازهٔ تاریخ) — ۴۰۰ با پیام فارسی.
/// </summary>
public sealed class TreasuryBankStatementFileInvalidException : Exception, IPublicProblemException
{
    public TreasuryBankStatementFileInvalidException(string publicDetail)
        : base(publicDetail)
    {
        PublicDetail = publicDetail;
    }

    public int StatusCode => 400;

    public string PublicDetail { get; }
}
