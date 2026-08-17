namespace Accounting.Domain.Exceptions;

/// <summary>
/// کد حساب خالی، با طول نادرست یا شامل کاراکتر غیرمجاز است.
/// توسط <see cref="ValueObjects.AccountCode"/> استفاده می‌شود.
/// </summary>
public sealed class InvalidAccountCodeException : DomainException
{
    public InvalidAccountCodeException(string message) : base(message)
    {
    }
}

/// <summary>
/// عنوان یک موجودیت خالی یا نامعتبر است. توسط <see cref="Common.Guard"/> استفاده می‌شود.
/// </summary>
public sealed class InvalidTitleException : DomainException
{
    public InvalidTitleException(string message) : base(message)
    {
    }
}
