using Accounting.Application.Tafsilis.Queries.GetTafsilis;

namespace Accounting.Application.Tests.Tafsilis.Queries.GetTafsilis;

public sealed class GetTafsilisQueryValidatorTests
{
    private readonly GetTafsilisQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidPaging_Passes()
    {
        var result = _validator.Validate(new GetTafsilisQuery(1, 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberZero_Fails()
    {
        var result = _validator.Validate(new GetTafsilisQuery(0, 20));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetTafsilisQuery(1, GetTafsilisQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetTafsilisQuery(1, GetTafsilisQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }
}
