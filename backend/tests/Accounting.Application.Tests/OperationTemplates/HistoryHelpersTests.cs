using Accounting.Application.OperationTemplates.History;

namespace Accounting.Application.Tests.OperationTemplates;

/// <summary>فاز ۶۳ — توابع قطعی یادگیری از سوابق: متن، امضا، شناسهٔ پایدار، درصد سهم‌ها.</summary>
public class HistoryHelpersTests
{
    // ─────────── متن ───────────

    [Fact]
    public void Normalize_UnifiesArabicLetters_DropsZwnjDiacriticsAndDigits()
    {
        Assert.Equal("کیف", HistoryText.Normalize("كيف"));
        Assert.Equal("میشود", HistoryText.Normalize("می‌شود"));
        Assert.Equal("کتاب", HistoryText.Normalize("کِتاب"));
        Assert.Equal("خرید     دارو", HistoryText.Normalize("خرید ۱۲۳ دارو")); // فاصله + ۳ رقم + فاصله
        Assert.Equal(string.Empty, HistoryText.Normalize("  "));
    }

    [Fact]
    public void Tokens_DropStopwordsAndShortWords_StemPlural()
    {
        var t = HistoryText.Tokens("پرداخت بابت خرید داروهای بیمارستان به شرکت و");
        Assert.Equal(new[] { "پرداخت", "خرید", "دارو", "بیمارستان", "شرکت" }, t);
    }

    [Fact]
    public void Tokens_PluralStemOnlyOnLongWords()
    {
        Assert.Contains("کالا", HistoryText.Tokens("کالاها"));
        Assert.Contains("ماها", HistoryText.Tokens("ماها")); // کوتاه ⇒ ریشه‌یابی نمی‌شود
    }

    [Fact]
    public void DistinctTokens_RemovesRepeats()
    {
        Assert.Single(HistoryText.DistinctTokens("دارو دارو داروها"));
    }

    [Fact]
    public void MaskForOtherUnits_CutsAtCounterpartyMarker()
    {
        Assert.Equal("پرداخت حق الزحمه", HistoryText.MaskForOtherUnits("پرداخت حق الزحمه به آقای محمدی ۱۲۰۰۰"));
        Assert.Equal("شرکت نفت", HistoryText.MaskForOtherUnits("شرکت نفت")); // نشانهٔ اول جمله برش نمی‌دهد
        Assert.Equal(10, HistoryText.MaskForOtherUnits("پرداخت حقوق کارکنان ماه مهر", 10).Length);
    }

    [Fact]
    public void Top_OrdersByCountThenAlphabetically()
    {
        var top = HistoryText.Top(new Dictionary<string, int> { ["ب"] = 2, ["الف"] = 2, ["ج"] = 5, ["د"] = 1 }, 3);
        Assert.Equal(new[] { "ج", "الف", "ب" }, top.Select(k => k.Word));
    }

    // ─────────── امضا و شناسهٔ پایدار ───────────

    [Fact]
    public void Signature_IsOrderIndependent_LowerCase_AndDeduplicated()
    {
        var a = Guid.Parse("BBBBBBBB-0000-0000-0000-000000000000");
        var b = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000000");
        var s1 = HistoryJson.SignatureOf([(a, 2), (b, 1), (a, 2)]);
        var s2 = HistoryJson.SignatureOf([(b, 1), (a, 2)]);
        Assert.Equal(s1, s2);
        Assert.Equal("aaaaaaaa-0000-0000-0000-000000000000:1|bbbbbbbb-0000-0000-0000-000000000000:2", s1);
    }

    [Fact]
    public void ParseSignature_RoundTrips_AndSkipsGarbage()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var sig = HistoryJson.SignatureOf([(a, 1), (b, 2)]) + "|bad|x:y";
        var parsed = RebuildHistoryPatternsHandler.ParseSignature(sig).ToList();
        Assert.Equal(2, parsed.Count);
        Assert.Contains((a, 1), parsed);
        Assert.Contains((b, 2), parsed);
    }

    [Fact]
    public void StableId_SameSignatureSameId()
    {
        Assert.Equal(RebuildHistoryPatternsHandler.StableId("x:1"), RebuildHistoryPatternsHandler.StableId("x:1"));
        Assert.NotEqual(RebuildHistoryPatternsHandler.StableId("x:1"), RebuildHistoryPatternsHandler.StableId("x:2"));
    }

    [Fact]
    public void HistoryCode_And_Title()
    {
        var id = Guid.NewGuid();
        Assert.True(HistoryDefaults.IsHistoryCode(HistoryDefaults.TemplateCode(id)));
        Assert.False(HistoryDefaults.IsHistoryCode("RECEIVE_CASH"));
        Assert.False(HistoryDefaults.IsHistoryCode(null));
        Assert.Equal("از سوابق: خرید", HistoryDefaults.Title(" خرید "));
        Assert.EndsWith("…", HistoryDefaults.Title(new string('ا', 80)));
    }

    // ─────────── JSON ستون‌ها ───────────

    [Fact]
    public void KeywordsFit_DropsFromEndUntilFits()
    {
        var list = Enumerable.Range(0, 50).Select(i => new KeywordCount("کلمه" + i, i)).ToList();
        var json = HistoryJson.KeywordsFit(list, 200)!;
        Assert.True(json.Length <= 200);
        var back = HistoryJson.Keywords(json);
        Assert.NotEmpty(back);
        Assert.Equal("کلمه0", back[0].Word);
        Assert.Null(HistoryJson.KeywordsFit(list, 5));
    }

    [Fact]
    public void BrokenJson_ParsesAsEmpty()
    {
        Assert.Empty(HistoryJson.Lines("{bad"));
        Assert.Empty(HistoryJson.Strings(null));
    }

    // ─────────── درصد سهم ردیف‌ها ───────────

    [Theory]
    [InlineData(new[] { 1.0 }, new[] { 100.0 })]
    [InlineData(new[] { 1.0, 1.0, 1.0 }, new[] { 33.34, 33.33, 33.33 })]
    [InlineData(new[] { 0.0, 0.0 }, new[] { 50.0, 50.0 })]
    [InlineData(new[] { 70.0, 20.0, 5.0 }, new[] { 73.69, 21.05, 5.26 })]
    public void Percents_SumToExactly100(double[] shares, double[] expected)
    {
        var r = HistoryTemplateFactory.Percents(shares.Select(s => (decimal)s).ToList());
        Assert.Equal(100m, r.Sum());
        Assert.Equal(expected.Select(e => (decimal)e), r);
    }

    [Fact]
    public void Percents_TinyShareKeepsMinimum()
    {
        var r = HistoryTemplateFactory.Percents([1_000_000m, 1m]);
        Assert.Equal(100m, r.Sum());
        Assert.True(r[1] >= 0.01m);
    }

    [Fact]
    public void Sanitize_RemovesBracesAndTruncates()
    {
        Assert.Equal("خرید amount", HistoryTemplateFactory.Sanitize(" خرید {amount} ", 50));
        Assert.Equal("abc", HistoryTemplateFactory.Sanitize("abcdef", 3));
        Assert.Equal("", HistoryTemplateFactory.Sanitize(null, 3));
    }
}
