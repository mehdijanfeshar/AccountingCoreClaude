namespace Accounting.Domain.Exceptions;

/// <summary>
/// کد سطحی از کدینگ (گروه/کل/معین) خالی، با طول نادرست یا شامل کاراکتر غیرمجاز است.
/// </summary>
public sealed class InvalidAccountCodeException : DomainException
{
    public InvalidAccountCodeException(string message) : base(message)
    {
    }
}

/// <summary>
/// تلاش برای تعریف یک کد تکراری زیر همان والد (مثلاً دو کل با یک کد در یک گروه).
/// </summary>
public sealed class DuplicateAccountCodeException : DomainException
{
    public DuplicateAccountCodeException(string message) : base(message)
    {
    }
}

/// <summary>
/// عنوان یک موجودیت (گروه/کل/معین/نوع تفصیلی/تفصیلی) خالی یا نامعتبر است.
/// </summary>
public sealed class InvalidTitleException : DomainException
{
    public InvalidTitleException(string message) : base(message)
    {
    }
}
