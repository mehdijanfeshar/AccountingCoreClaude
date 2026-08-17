namespace Accounting.Domain.Exceptions;

/// <summary>
/// شمارهٔ سند خالی یا نامعتبر است.
/// </summary>
public sealed class InvalidVoucherNumberException : DomainException
{
    public InvalidVoucherNumberException(string message) : base(message)
    {
    }
}

/// <summary>
/// سند بدون هیچ ردیفی تلاش می‌کند Post شود (قانون ۶).
/// </summary>
public sealed class EmptyVoucherException : DomainException
{
    public EmptyVoucherException(string voucherNumber)
        : base($"سند شماره «{voucherNumber}» بدون هیچ ردیفی قابل صدور (Post) نیست.")
    {
    }
}

/// <summary>
/// جمع بدهکار و بستانکار سند در زمان Post برابر نیست (قانون ۶).
/// </summary>
public sealed class VoucherNotBalancedException : DomainException
{
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }

    public VoucherNotBalancedException(string voucherNumber, decimal totalDebit, decimal totalCredit)
        : base($"سند شماره «{voucherNumber}» تراز نیست: جمع بدهکار = {totalDebit}، جمع بستانکار = {totalCredit}.")
    {
        TotalDebit = totalDebit;
        TotalCredit = totalCredit;
    }
}

/// <summary>
/// تلاش برای تغییر سندی که قبلاً Post شده و طبق قانون تغییرناپذیر (immutable) است.
/// </summary>
public sealed class VoucherImmutableException : DomainException
{
    public VoucherImmutableException(string voucherNumber)
        : base($"سند شماره «{voucherNumber}» صادر (Post) شده و غیرقابل تغییر است.")
    {
    }
}
