namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>
/// خطای نحوی انتخاب‌گر حساب یا فرمول ردیف قالب صورت مالی. پیام فارسی و قابل نمایش به کاربر مالی است
/// و موقعیت (۰-مبنا) نویسهٔ مشکل‌دار را همراه دارد.
/// </summary>
public sealed class FsExpressionException : Exception
{
    public FsExpressionException(string message, int position)
        : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}
