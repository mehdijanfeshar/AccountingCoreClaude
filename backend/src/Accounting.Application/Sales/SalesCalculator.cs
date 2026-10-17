using Accounting.Domain.Sales;

namespace Accounting.Application.Sales;

/// <summary>
/// محاسبهٔ سطری سند فروش (تابع خالص، ریال صحیح): ناخالص = تعداد × فی؛ تخفیف ردیف = ناخالص × درصد؛ تخفیف کلی
/// به نسبت خالص ردیف‌ها سرشکن و باقیماندهٔ گرد روی آخرین ردیف غیرصفر؛ مالیات = round(مشمول × نرخ) جز کالای معاف.
/// گردکردن در سطح ردیف — مطابق صورتحساب الکترونیکی.
/// </summary>
public static class SalesCalculator
{
    public sealed record Totals(long Gross, long Discount, long Taxable, long Vat, long Total);

    /// <summary>جمع‌ها و مبالغ هر ردیف را روی خود <paramref name="lines"/> می‌نویسد.</summary>
    public static Totals Apply(IList<SalesDocLine> lines, long headerDiscount, decimal vatRate, Func<Guid, bool> isExempt)
    {
        foreach (var l in lines)
        {
            l.Gross = Round(l.Qty * l.Price);
            l.LineDiscount = Round(l.Gross * Math.Clamp(l.DiscountPercent, 0, 100) / 100m);
            l.HeaderDiscountShare = 0;
        }

        var totalNet = lines.Sum(l => l.Gross - l.LineDiscount);
        var hd = Math.Clamp(headerDiscount, 0, Math.Max(0, totalNet));
        if (hd > 0 && totalNet > 0)
        {
            long given = 0;
            foreach (var l in lines)
            {
                l.HeaderDiscountShare = (long)Math.Floor(hd * (decimal)(l.Gross - l.LineDiscount) / totalNet);
                given += l.HeaderDiscountShare;
            }
            var last = lines.LastOrDefault(l => l.Gross - l.LineDiscount > 0);
            if (last is not null) last.HeaderDiscountShare += hd - given;
        }

        foreach (var l in lines)
        {
            l.Taxable = l.Gross - l.LineDiscount - l.HeaderDiscountShare;
            l.VatRate = isExempt(l.ItemId) ? 0 : vatRate;
            l.Vat = Round(l.Taxable * l.VatRate / 100m);
            l.Total = l.Taxable + l.Vat;
        }

        return new Totals(
            lines.Sum(l => l.Gross),
            lines.Sum(l => l.LineDiscount + l.HeaderDiscountShare),
            lines.Sum(l => l.Taxable),
            lines.Sum(l => l.Vat),
            lines.Sum(l => l.Total));
    }

    /// <summary>نرخ آخرین ردیفی که تاریخ شروعش ≤ تاریخ سند است؛ هیچ ⇒ صفر.</summary>
    public static decimal VatRateFor(IEnumerable<SalesVatRate> rates, string docDate) =>
        rates.Where(r => !r.IsDeleted && string.CompareOrdinal(r.FromDate, docDate) <= 0)
             .OrderByDescending(r => r.FromDate)
             .Select(r => r.Rate)
             .FirstOrDefault();

    private static long Round(decimal v) => (long)Math.Round(v, MidpointRounding.AwayFromZero);
}
