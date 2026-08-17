namespace Accounting.Domain.Exceptions;

/// <summary>
/// یک نوع تفصیلی بیش از یک بار به همان معین لینک شده است (قانون ۳).
/// </summary>
public sealed class DuplicateDetailTypeLinkException : DomainException
{
    public DuplicateDetailTypeLinkException(string message) : base(message)
    {
    }
}
