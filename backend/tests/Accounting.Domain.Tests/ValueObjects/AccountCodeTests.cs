using Accounting.Domain.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Tests.ValueObjects;

/// <summary>پوشش بند ۱۰: کد خالی، طول اشتباه، کاراکتر غیررقمی.</summary>
public class AccountCodeTests
{
    [Fact]
    public void Create_WithEmptyValue_ThrowsInvalidAccountCodeException()
    {
        Assert.Throws<InvalidAccountCodeException>(() => AccountCode.Create("", 3));
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsInvalidAccountCodeException()
    {
        Assert.Throws<InvalidAccountCodeException>(() => AccountCode.Create("   ", 3));
    }

    [Fact]
    public void Create_WithNullValue_ThrowsInvalidAccountCodeException()
    {
        Assert.Throws<InvalidAccountCodeException>(() => AccountCode.Create(null, 3));
    }

    [Fact]
    public void Create_WithWrongLength_ThrowsInvalidAccountCodeException()
    {
        Assert.Throws<InvalidAccountCodeException>(() => AccountCode.Create("12", 3));
    }

    [Fact]
    public void Create_WithNonDigitCharacter_ThrowsInvalidAccountCodeException()
    {
        Assert.Throws<InvalidAccountCodeException>(() => AccountCode.Create("1A2", 3));
    }

    [Fact]
    public void Create_WithValidDigitsAndLength_Succeeds()
    {
        var code = AccountCode.Create("123", 3);

        Assert.Equal("123", code.Value);
    }

    [Fact]
    public void Create_TrimsWhitespaceAroundValidCode()
    {
        var code = AccountCode.Create("  123  ", 3);

        Assert.Equal("123", code.Value);
    }

    [Fact]
    public void TwoCodes_WithSameValue_AreEqual()
    {
        var a = AccountCode.Create("123", 3);
        var b = AccountCode.Create("123", 3);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
