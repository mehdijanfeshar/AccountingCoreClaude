using Accounting.Domain.Accrual;

namespace Accounting.Domain.Tests.Accrual;

public class AccrualScheduleSplitterTests
{
    [Fact]
    public void Even_split_twelve_months()
    {
        var shares = AccrualScheduleSplitter.Split(120_000_000, 12);

        Assert.Equal(12, shares.Count);
        Assert.All(shares, s => Assert.Equal(10_000_000, s));
        Assert.Equal(120_000_000, shares.Sum());
    }

    [Fact]
    public void Remainder_goes_to_last_line()
    {
        var shares = AccrualScheduleSplitter.Split(100_000_001, 3);

        Assert.Equal([33_333_333, 33_333_333, 33_333_335], shares);
        Assert.Equal(100_000_001, shares.Sum());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(999_999_999_999, 120)]
    [InlineData(80_000_000, 1)]
    [InlineData(1_000_000_007, 11)]
    public void Sum_always_equals_total(long total, int parts)
    {
        var shares = AccrualScheduleSplitter.Split(total, parts);

        Assert.Equal(parts, shares.Count);
        Assert.Equal(total, shares.Sum());
        Assert.All(shares, s => Assert.True(s > 0));
    }

    [Fact]
    public void Rejects_zero_parts_and_amount_below_parts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AccrualScheduleSplitter.Split(100, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => AccrualScheduleSplitter.Split(2, 3));
    }
}
