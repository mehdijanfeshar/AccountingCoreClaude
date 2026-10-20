using Accounting.Application.Reports.AttributeAccountReconciliation;

namespace Accounting.Application.Tests.Reports.AttributeAccountReconciliation;

/// <summary>FINACC-523 — تعریف مغایرت حساب‌های شناسه‌دار (ریسک #۳۰).</summary>
public class AttributeReconciliationCalculatorTests
{
    private static readonly Guid AccA = Guid.NewGuid();
    private static readonly Guid AccB = Guid.NewGuid();

    /// <param name="attribSum">۱ = جمع‌پذیر، ۲ = جمع‌ناپذیر</param>
    private static AttributeAccountRawLine L(string? value, decimal debtor, decimal creditor, int attribSum = 1, Guid? acc = null, string code = "110101") =>
        new(Guid.NewGuid(), acc ?? AccA, code, "حساب", attribSum, value, Guid.NewGuid(), "1", "14050101", 2, null, null, debtor, creditor);

    private static AttributeAccountValueDto One(IEnumerable<AttributeAccountRawLine> lines, string? value) =>
        AttributeReconciliationCalculator.Values(lines).Single(v => v.AttributeValue == value);

    [Fact]
    public void Summable_BalancedValue_IsNotMismatch()
    {
        var v = One([L("A1", 100, 0), L("A1", 0, 60), L("A1", 0, 40)], "A1");
        Assert.False(v.IsMismatch);
        Assert.Null(v.Reason);
        Assert.Equal(3, v.LineCount);
    }

    [Fact]
    public void UnbalancedValue_IsBalanceMismatch()
    {
        var v = One([L("A1", 100, 0), L("A1", 0, 90)], "A1");
        Assert.Equal(AttributeMismatchReason.Balance, v.Reason);
    }

    [Fact]
    public void NonSummable_PartialSettlement_IsUnpaired()
    {
        // بدهکار ۱۰۰ با دو بستانکار ۵۰ — جمع برابر ولی جفت نمی‌شود.
        var v = One([L("C1", 100, 0, 2), L("C1", 0, 50, 2), L("C1", 0, 50, 2)], "C1");
        Assert.Equal(AttributeMismatchReason.UnpairedAmounts, v.Reason);
    }

    [Fact]
    public void NonSummable_OneToOnePairs_IsNotMismatch()
    {
        var v = One([L("C1", 100, 0, 2), L("C1", 0, 70, 2), L("C1", 70, 0, 2), L("C1", 0, 100, 2)], "C1");
        Assert.False(v.IsMismatch);
    }

    [Fact]
    public void LinesWithoutIdentifier_AreAlwaysMismatch_EvenIfBalanced()
    {
        var v = One([L(null, 100, 0), L("  ", 0, 100)], null);
        Assert.Equal(AttributeMismatchReason.MissingIdentifier, v.Reason);
        Assert.Equal(2, v.LineCount);
    }

    [Fact]
    public void ValuesAreTrimmedBeforeGrouping()
    {
        var values = AttributeReconciliationCalculator.Values([L(" A1 ", 100, 0), L("A1", 0, 100)]);
        Assert.False(Assert.Single(values).IsMismatch);
    }

    [Fact]
    public void Values_OrderMismatchesFirst_MissingIdentifierOnTop()
    {
        var values = AttributeReconciliationCalculator.Values(
            [L("Z", 10, 10), L("B", 10, 0), L(null, 5, 0), L("A", 10, 10)]);
        Assert.Equal(new string?[] { null, "B", "A", "Z" }, values.Select(v => v.AttributeValue));
    }

    [Fact]
    public void Moeins_SummarisePerAccount_OrderedByCode()
    {
        var lines = new[]
        {
            L("X", 100, 0, acc: AccB, code: "220101"), L("X", 0, 100, acc: AccB, code: "220101"),
            L("A1", 100, 0), L("A2", 0, 40), L(null, 0, 10),
        };

        var moeins = AttributeReconciliationCalculator.Moeins(lines);

        Assert.Equal(new[] { "110101", "220101" }, moeins.Select(m => m.AccCode));
        var a = moeins[0];
        Assert.Equal(100, a.Debtor);
        Assert.Equal(50, a.Creditor);
        Assert.Equal(2, a.IdentifierCount);      // A1 و A2 (بی‌شناسه شمرده نمی‌شود)
        Assert.Equal(3, a.MismatchCount);        // A1، A2 و بی‌شناسه
        Assert.Equal(1, a.LinesWithoutIdentifier);
        Assert.Equal(0, moeins[1].MismatchCount);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" x ", "x")]
    public void NormalizeValue(string? input, string? expected) =>
        Assert.Equal(expected, AttributeReconciliationCalculator.NormalizeValue(input));
}
