using Accounting.Domain.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Tests.ValueObjects;

/// <summary>بخشی از پوشش بند ۶: Money.Of با مقدار منفی باید NegativeAmountException بدهد.</summary>
public class MoneyTests
{
    [Fact]
    public void Of_WithNegativeAmount_ThrowsNegativeAmountException()
    {
        Assert.Throws<NegativeAmountException>(() => Money.Of(-1m));
    }

    [Fact]
    public void Of_WithZero_Succeeds()
    {
        var money = Money.Of(0m);

        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Zero_HasAmountZero()
    {
        Assert.Equal(0m, Money.Zero.Amount);
    }

    [Fact]
    public void Addition_SumsAmountsCorrectly()
    {
        var a = Money.Of(100m);
        var b = Money.Of(50.5m);

        var sum = a + b;

        Assert.Equal(150.5m, sum.Amount);
    }
}
