using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;

public sealed class GetWhiteAndBlackListByIdQueryValidatorTests
{
    private readonly GetWhiteAndBlackListByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetWhiteAndBlackListByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetWhiteAndBlackListByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWhiteAndBlackListByIdQuery.Id));
    }
}
