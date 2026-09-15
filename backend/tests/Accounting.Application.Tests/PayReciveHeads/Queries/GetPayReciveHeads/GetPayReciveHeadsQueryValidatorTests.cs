using Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeads;

namespace Accounting.Application.Tests.PayReciveHeads.Queries.GetPayReciveHeads;

public sealed class GetPayReciveHeadsQueryValidatorTests
{
    private readonly GetPayReciveHeadsQueryValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(5, GetPayReciveHeadsQueryValidator.MaxPageSize)]
    [InlineData(GetPayReciveHeadsQueryValidator.MaxPageNumber, 20)]
    public void Validate_WithinBounds_Passes(int pageNumber, int pageSize)
    {
        Assert.True(_validator.Validate(new GetPayReciveHeadsQuery(pageNumber, pageSize)).IsValid);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, GetPayReciveHeadsQueryValidator.MaxPageSize + 1)]
    [InlineData(GetPayReciveHeadsQueryValidator.MaxPageNumber + 1, 20)]
    public void Validate_OutOfBounds_Fails(int pageNumber, int pageSize)
    {
        Assert.False(_validator.Validate(new GetPayReciveHeadsQuery(pageNumber, pageSize)).IsValid);
    }

    /// <summary>
    /// Pins the overflow guarantee documented on the validator: the largest accepted
    /// <c>(pageNumber - 1) * pageSize</c> must still fit in an <see cref="int"/>, so the
    /// repository's <c>Skip(...)</c> can never wrap around into a negative offset.
    /// </summary>
    [Fact]
    public void MaxPageNumberTimesMaxPageSize_CannotOverflowSkip()
    {
        var largestSkip = (long)(GetPayReciveHeadsQueryValidator.MaxPageNumber - 1)
            * GetPayReciveHeadsQueryValidator.MaxPageSize;

        Assert.True(largestSkip <= int.MaxValue);
    }
}
