using Accounting.Application.OperationTemplates.Agent;

namespace Accounting.Application.Tests.OperationTemplates;

public class PiiMaskerTests
{
    [Fact]
    public void ValidNationalCode_IsMasked_AndRestored()
    {
        var r = PiiMasker.Mask("پرداخت به کد ملی 1234567891 انجام شد");
        Assert.Equal("پرداخت به کد ملی [[NID_1]] انجام شد", r.Masked);
        Assert.True(r.HasMasked);
        Assert.Equal("{\"nid\":\"1234567891\"}", r.Restore("{\"nid\":\"[[NID_1]]\"}"));
    }

    [Fact]
    public void PersianDigitNationalCode_IsMasked()
    {
        var r = PiiMasker.Mask("کد ملی ۱۲۳۴۵۶۷۸۹۱");
        Assert.Equal("کد ملی [[NID_1]]", r.Masked);
        Assert.Equal("۱۲۳۴۵۶۷۸۹۱", r.Map["[[NID_1]]"]);
    }

    [Theory]
    [InlineData("مبلغ 1000000000 ریال")] // مبلغ ۱۰ رقمی — رقم کنترل نامعتبر
    [InlineData("کد 1234567890")]         // رقم کنترل غلط
    [InlineData("کد 1111111111")]         // همهٔ ارقام یکسان
    public void TenDigitsWithoutValidCheckDigit_AreNotMasked(string text)
    {
        var r = PiiMasker.Mask(text);
        Assert.Equal(text, r.Masked);
        Assert.False(r.HasMasked);
    }

    [Fact]
    public void Iban_Card_Phone_AreMaskedWithOwnTokens()
    {
        var r = PiiMasker.Mask("شبا IR820540102680020817909002 کارت 6037-9911-2345-6789 تلفن 09121234567");
        Assert.Equal("شبا [[IBAN_1]] کارت [[CARD_1]] تلفن [[TEL_1]]", r.Masked);
        Assert.Equal("IR820540102680020817909002", r.Map["[[IBAN_1]]"]);
        Assert.Equal("6037-9911-2345-6789", r.Map["[[CARD_1]]"]);
        Assert.Equal("09121234567", r.Map["[[TEL_1]]"]);
    }

    [Fact]
    public void RepeatedKinds_AreNumbered()
    {
        var r = PiiMasker.Mask("09121234567 و 09351234567");
        Assert.Equal("[[TEL_1]] و [[TEL_2]]", r.Masked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_ReturnsEmpty(string? text)
    {
        var r = PiiMasker.Mask(text);
        Assert.Equal("", r.Masked);
        Assert.False(r.HasMasked);
        Assert.Equal("x", r.Restore("x"));
    }
}
