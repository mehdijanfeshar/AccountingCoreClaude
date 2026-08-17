namespace Accounting.Domain.Exceptions;

/// <summary>
/// کلاس پایهٔ همهٔ خطاهای نقض قوانین کسب‌وکار در لایهٔ Domain.
/// هرگز نباید <see cref="Exception"/> خام پرتاب شود یا bool برای اعلام خطا برگردانده شود؛
/// همهٔ نقض قوانین دامنه باید از طریق زیرکلاس‌های این نوع گزارش شوند.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
