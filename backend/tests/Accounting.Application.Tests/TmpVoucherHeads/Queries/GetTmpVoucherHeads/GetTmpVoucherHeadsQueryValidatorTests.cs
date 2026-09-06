using Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeads;

namespace Accounting.Application.Tests.TmpVoucherHeads.Queries.GetTmpVoucherHeads;

public sealed class GetTmpVoucherHeadsQueryValidatorTests
{
    private readonly GetTmpVoucherHeadsQueryValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(5, GetTmpVoucherHeadsQueryValidator.MaxPageSize)]
    [InlineData(GetTmpVoucherHeadsQueryValidator.MaxPageNumber, 20)]
    public void Validate_WithinBounds_Passes(int pageNumber, int pageSize)
    {
        Assert.True(_validator.Validate(new GetTmpVoucherHeadsQuery(pageNumber, pageSize)).IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, GetTmpVoucherHeadsQueryValidator.MaxPageSize + 1)]
    [InlineData(GetTmpVoucherHeadsQueryValidator.MaxPageNumber + 1, 20)]
    public void Validate_OutOfBounds_Fails(int pageNumber, int pageSize)
    {
        Assert.False(_validator.Validate(new GetTmpVoucherHeadsQuery(pageNumber, pageSize)).IsValid);
    }

    /// <summary>
    /// Pins the overflow guarantee documented on the validator: the largest accepted
    /// <c>(pageNumber - 1) * pageSize</c> must still fit in an <see cref="int"/>, so the
    /// repository's <c>Skip(...)</c> can never wrap around into a negative offset.
    /// </summary>
    [Fact]
    public void MaxPageNumberTimesMaxPageSize_CannotOverflowSkip()
    {
        var largestSkip = (long)(GetTmpVoucherHeadsQueryValidator.MaxPageNumber - 1)
            * GetTmpVoucherHeadsQueryValidator.MaxPageSize;

        Assert.True(largestSkip <= int.MaxValue);
    }
}
