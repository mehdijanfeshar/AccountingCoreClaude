using Accounting.Application.FinancialStatements.Engine;

namespace Accounting.Application.Tests.FinancialStatements.Engine;

/// <summary>انتخاب‌گر حساب ردیف Account (دقیق، پیشوند، بازه، استثنا، محدودیت مانده).</summary>
public sealed class AccountSelectorTests
{
    [Fact]
    public void Parse_Exact_Prefix_Range()
    {
        var sel = AccountSelector.Parse("1301 13* 1401..1409");

        Assert.Equal(3, sel.Terms.Count);
        Assert.Equal(SelectorTermKind.Exact, sel.Terms[0].Kind);
        Assert.Equal(SelectorTermKind.Prefix, sel.Terms[1].Kind);
        Assert.Equal(SelectorTermKind.Range, sel.Terms[2].Kind);
        Assert.Equal("1409", sel.Terms[2].To);
    }

    [Fact]
    public void Parse_AcceptsCommaAndPersianCommaAndPersianDigits()
    {
        var sel = AccountSelector.Parse("۱۳*،۱۴۰۱, 15*");

        Assert.Equal(new[] { "13", "1401", "15" }, sel.Terms.Select(t => t.From).ToArray());
    }

    [Fact]
    public void Parse_ExcludeAndSideMarkers()
    {
        var sel = AccountSelector.Parse("13*[D] !1305 14*[c]");

        Assert.Equal(SelectorBalanceSide.DebitOnly, sel.Terms[0].Side);
        Assert.True(sel.Terms[1].Exclude);
        Assert.Equal(SelectorBalanceSide.CreditOnly, sel.Terms[2].Side);
        Assert.Equal(2, sel.Includes.Count());
        Assert.Single(sel.Excludes);
    }

    [Fact]
    public void ToString_RoundTripsNormalizedForm()
    {
        var sel = AccountSelector.Parse("۱۳*,  !1305,1401..1409[C]");

        Assert.Equal("13* !1305 1401..1409[C]", sel.ToString());
        Assert.Equal(sel.ToString(), AccountSelector.Parse(sel.ToString()).ToString());
    }

    [Theory]
    [InlineData("1301", "1301", true)]
    [InlineData("1301", "13010", false)]      // دقیق است، نه پیشوند
    [InlineData("13*", "1399", true)]
    [InlineData("13*", "1499", false)]
    [InlineData("1301..1309", "1301", true)]
    [InlineData("1301..1309", "1309", true)]
    [InlineData("1301..1309", "1305", true)]
    [InlineData("1301..1309", "13051", true)]  // پیشوند هم‌طول مقایسه می‌شود
    [InlineData("1301..1309", "1310", false)]
    [InlineData("1301..1309", "1300", false)]
    [InlineData("1301..1309", "130", false)]   // کوتاه‌تر از طول بازه
    public void Match_ByKind(string selector, string code, bool expected)
    {
        Assert.Equal(expected, AccountSelector.Parse(selector).Match(code) is not null);
    }

    [Fact]
    public void Match_ExcludeWinsOverInclude()
    {
        var sel = AccountSelector.Parse("13* !1305");

        Assert.NotNull(sel.Match("1301"));
        Assert.Null(sel.Match("1305"));
    }

    [Fact]
    public void Match_ReturnsTermCarryingBalanceSide()
    {
        var sel = AccountSelector.Parse("13*[D]");

        Assert.Equal(SelectorBalanceSide.DebitOnly, sel.Match("1301")!.Side);
    }

    [Fact]
    public void Match_ExcludeWithPrefix_RemovesWholeSubtree()
    {
        var sel = AccountSelector.Parse("1* !13*");

        Assert.NotNull(sel.Match("1201"));
        Assert.Null(sel.Match("1399"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ,  ")]
    [InlineData("!13*")]                // فقط استثنا
    [InlineData("!")]                   // کد ندارد
    [InlineData("13..130")]             // سرها هم‌طول نیستند
    [InlineData("1309..1301")]          // بازهٔ معکوس
    [InlineData("13[X]")]               // محدودیت مانده نامعتبر
    [InlineData("13[D")]                // ناقص
    [InlineData("13{a=1}")]             // فیلتر تفصیلی هنوز نه
    [InlineData("13#")]                 // نویسهٔ نامعتبر
    [InlineData("*")]                   // پیشوند بدون کد
    public void TryParse_Invalid_ReturnsFalse(string? text)
    {
        var ok = AccountSelector.TryParse(text, out var sel, out var error);

        Assert.False(ok);
        Assert.Null(sel);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void FromTerms_BuildsEquivalentSelector()
    {
        var parsed = AccountSelector.Parse("13* !1305");

        var rebuilt = AccountSelector.FromTerms(parsed.Terms);

        Assert.Equal(parsed.ToString(), rebuilt.ToString());
        Assert.Null(rebuilt.Match("1305"));
    }
}
