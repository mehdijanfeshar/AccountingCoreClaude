using Accounting.Application.RevolvingFunds.Queries.GetRevolvingFundById;

namespace Accounting.Application.Tests.RevolvingFunds.Queries.GetRevolvingFundById;

public sealed class GetRevolvingFundByIdQueryValidatorTests
{
    private readonly GetRevolvingFundByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetRevolvingFundByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetRevolvingFundByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetRevolvingFundByIdQuery.Id));
    }
}
