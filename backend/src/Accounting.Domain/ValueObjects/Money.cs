using Accounting.Domain.Exceptions;

namespace Accounting.Domain.ValueObjects;

/// <summary>
/// یک مبلغ پولی نامنفی. طبق قانون ۶، مقادیر منفی برای بدهکار/بستانکار ممنوع است؛
/// این محدودیت در همین Value Object و در لحظهٔ ساخت اعمال می‌شود تا ساختن یک مبلغ
/// منفی از اساس غیرممکن باشد.
/// </summary>
public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }

    public static Money Zero { get; } = new(0m);

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Of(decimal amount)
    {
        if (amount < 0)
            throw new NegativeAmountException(amount);

        return new Money(amount);
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static bool operator ==(Money? left, Money? right) => Equals(left, right);

    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public int CompareTo(Money? other) => Amount.CompareTo(other?.Amount ?? 0m);

    public bool Equals(Money? other) => other is not null && Amount == other.Amount;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => Amount.GetHashCode();

    public override string ToString() => Amount.ToString("0.####");
}
