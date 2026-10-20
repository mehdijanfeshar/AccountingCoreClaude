using Accounting.Application.Sales;
using Accounting.Domain.Sales;

namespace Accounting.Application.Tests.Sales;

/// <summary>فاز ۵۹ — محاسبهٔ سطری سند فروش (ریال صحیح، گرد در سطح ردیف).</summary>
public class SalesCalculatorTests
{
    private static readonly Guid Exempt = Guid.NewGuid();

    private static SalesDocLine L(decimal qty, long price, decimal discountPercent = 0, Guid? item = null) =>
        new() { Id = Guid.NewGuid(), ItemId = item ?? Guid.NewGuid(), Qty = qty, Price = price, DiscountPercent = discountPercent };

    private static SalesCalculator.Totals Apply(List<SalesDocLine> lines, long headerDiscount = 0, decimal vat = 10) =>
        SalesCalculator.Apply(lines, headerDiscount, vat, id => id == Exempt);

    [Fact]
    public void SingleLine_GrossDiscountVatTotal()
    {
        var line = L(3, 1_000, discountPercent: 10);
        var t = Apply([line]);

        Assert.Equal(3_000, line.Gross);
        Assert.Equal(300, line.LineDiscount);
        Assert.Equal(2_700, line.Taxable);
        Assert.Equal(10, line.VatRate);
        Assert.Equal(270, line.Vat);
        Assert.Equal(2_970, line.Total);
        Assert.Equal(new SalesCalculator.Totals(3_000, 300, 2_700, 270, 2_970), t);
    }

    [Fact]
    public void FractionalQuantity_RoundsAwayFromZero()
    {
        var line = L(1.5m, 333); // 499.5 ⇒ 500
        Apply([line], vat: 0);
        Assert.Equal(500, line.Gross);
    }

    [Fact]
    public void HeaderDiscount_SpreadProportionally_RemainderOnLastNonZeroLine()
    {
        var a = L(1, 1_000);
        var b = L(1, 2_000);
        var zero = L(0, 5_000);
        var t = Apply([a, b, zero], headerDiscount: 100, vat: 0);

        Assert.Equal(33, a.HeaderDiscountShare);   // floor(100 × 1000/3000)
        Assert.Equal(67, b.HeaderDiscountShare);   // 66 + باقیماندهٔ ۱
        Assert.Equal(0, zero.HeaderDiscountShare);
        Assert.Equal(100, t.Discount);
        Assert.Equal(2_900, t.Taxable);
    }

    [Fact]
    public void HeaderDiscount_ClampedToNet()
    {
        var a = L(1, 1_000);
        var t = Apply([a], headerDiscount: 5_000, vat: 10);
        Assert.Equal(1_000, a.HeaderDiscountShare);
        Assert.Equal(0, t.Taxable);
        Assert.Equal(0, t.Vat);
        Assert.Equal(0, t.Total);
    }

    [Fact]
    public void NegativeHeaderDiscount_IsIgnored()
    {
        var a = L(1, 1_000);
        Apply([a], headerDiscount: -50, vat: 0);
        Assert.Equal(0, a.HeaderDiscountShare);
        Assert.Equal(1_000, a.Total);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(150, 1_000)]
    public void LineDiscountPercent_IsClampedTo0And100(decimal percent, long expectedDiscount)
    {
        var a = L(1, 1_000, percent);
        Apply([a], vat: 0);
        Assert.Equal(expectedDiscount, a.LineDiscount);
    }

    [Fact]
    public void ExemptItem_HasNoVat()
    {
        var taxed = L(1, 1_000);
        var exempt = L(1, 1_000, item: Exempt);
        var t = Apply([taxed, exempt], vat: 10);
        Assert.Equal(100, taxed.Vat);
        Assert.Equal(0, exempt.Vat);
        Assert.Equal(0, exempt.VatRate);
        Assert.Equal(100, t.Vat);
        Assert.Equal(2_100, t.Total);
    }

    [Fact]
    public void VatRoundedPerLine_NotOnTotal()
    {
        // هر ردیف ۱۵ ریال × ۱۰٪ = ۱.۵ ⇒ ۲ ؛ جمع ۴ (نه round(3) = 3)
        var t = Apply([L(1, 15), L(1, 15)], vat: 10);
        Assert.Equal(4, t.Vat);
    }

    [Fact]
    public void Reapply_ResetsPreviousHeaderShares()
    {
        var a = L(1, 1_000);
        Apply([a], headerDiscount: 100, vat: 0);
        Apply([a], headerDiscount: 0, vat: 0);
        Assert.Equal(0, a.HeaderDiscountShare);
        Assert.Equal(1_000, a.Total);
    }

    [Fact]
    public void VatRateFor_PicksLatestEffectiveNonDeletedRate()
    {
        var rates = new List<SalesVatRate>
        {
            new() { FromDate = "14020101", Rate = 9 },
            new() { FromDate = "14030101", Rate = 10 },
            new() { FromDate = "14040101", Rate = 12, IsDeleted = true },
            new() { FromDate = "14050101", Rate = 15 },
        };
        Assert.Equal(10, SalesCalculator.VatRateFor(rates, "14041230"));
        Assert.Equal(15, SalesCalculator.VatRateFor(rates, "14050101"));
        Assert.Equal(9, SalesCalculator.VatRateFor(rates, "14021229"));
        Assert.Equal(0, SalesCalculator.VatRateFor(rates, "14011229"));
    }
}
