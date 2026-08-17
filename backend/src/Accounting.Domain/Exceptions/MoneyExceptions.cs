namespace Accounting.Domain.Exceptions;

/// <summary>
/// تلاش برای ساخت یک مبلغ (Money) با مقدار منفی.
/// </summary>
public sealed class NegativeAmountException : DomainException
{
    public NegativeAmountException(decimal amount)
        : base($"مبلغ نمی‌تواند منفی باشد: {amount}")
    {
    }
}
