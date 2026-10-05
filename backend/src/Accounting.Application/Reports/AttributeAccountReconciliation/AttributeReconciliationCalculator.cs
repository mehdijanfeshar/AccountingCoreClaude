namespace Accounting.Application.Reports.AttributeAccountReconciliation;

/// <summary>
/// منطق مغایرت‌گیری، جدا از دسترسی به داده.
///
/// <para>
/// <b>تعریف مغایرت (هر شناسه = معین + مقدار شناسه):</b>
/// جمع‌پذیر ⇒ مغایر اگر Σبدهکار ≠ Σبستانکار.
/// جمع‌ناپذیر ⇒ علاوه بر آن، هر مبلغ بدهکار باید دقیقاً با یک مبلغ بستانکار برابر جفت شود
/// (چندمجموعهٔ مبالغ بدهکار = چندمجموعهٔ مبالغ بستانکار)؛ تسویهٔ یک بدهی با چند پرداخت جزئی مغایرت است.
/// ردیف‌های بی‌شناسه روی حساب شناسه‌دار همیشه مغایرت‌اند.
/// </para>
///
/// <para>
/// Viewهای مرجع (<c>VW_ATTRIBMOINREPORT</c>، <c>VW_ATTRIBVALUEBYMOINREPORT</c>،
/// <c>VW_ATTRIBMISMATCHREPORT</c>) این را درست حساب نمی‌کنند — ریسک #۳۰ در
/// <c>docs/open-decisions.md</c>.
/// </para>
/// </summary>
public static class AttributeReconciliationCalculator
{
    public static string? NormalizeValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static IReadOnlyList<AttributeAccountMoeinDto> Moeins(IEnumerable<AttributeAccountRawLine> lines)
        => lines
            .GroupBy(l => l.AccountId)
            .Select(g =>
            {
                var first = g.First();
                var values = Values(g);
                return new AttributeAccountMoeinDto(
                    g.Key,
                    first.AccCode,
                    first.AccName,
                    first.AttribSum,
                    g.Sum(l => l.Debtor),
                    g.Sum(l => l.Creditor),
                    values.Count(v => v.AttributeValue is not null),
                    values.Count(v => v.IsMismatch),
                    g.Count(l => NormalizeValue(l.AttributeValue) is null));
            })
            .OrderBy(m => m.AccCode, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<AttributeAccountValueDto> Values(IEnumerable<AttributeAccountRawLine> lines)
        => lines
            .GroupBy(l => NormalizeValue(l.AttributeValue))
            .Select(g =>
            {
                var debtor = g.Sum(l => l.Debtor);
                var creditor = g.Sum(l => l.Creditor);
                var reason = Evaluate(g.Key, g.First().AttribSum, g.ToList(), debtor, creditor);
                return new AttributeAccountValueDto(g.Key, debtor, creditor, g.Count(), reason is not null, reason);
            })
            // مغایرها اول، «بدون شناسه» در صدر مغایرها، سپس مقدار شناسه.
            .OrderByDescending(v => v.IsMismatch)
            .ThenBy(v => v.AttributeValue is not null)
            .ThenBy(v => v.AttributeValue, StringComparer.Ordinal)
            .ToList();

    private static AttributeMismatchReason? Evaluate(
        string? value, int attribSum, List<AttributeAccountRawLine> lines, decimal debtor, decimal creditor)
    {
        if (value is null)
            return AttributeMismatchReason.MissingIdentifier;
        if (debtor != creditor)
            return AttributeMismatchReason.Balance;
        if (attribSum == 2)
        {
            var d = lines.Where(l => l.Debtor != 0).Select(l => l.Debtor).Order().ToList();
            var c = lines.Where(l => l.Creditor != 0).Select(l => l.Creditor).Order().ToList();
            if (!d.SequenceEqual(c))
                return AttributeMismatchReason.UnpairedAmounts;
        }
        return null;
    }
}
