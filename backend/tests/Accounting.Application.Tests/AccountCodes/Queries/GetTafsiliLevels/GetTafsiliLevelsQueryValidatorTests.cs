using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;

namespace Accounting.Application.Tests.AccountCodes.Queries.GetTafsiliLevels;

public sealed class GetTafsiliLevelsQueryValidatorTests
{
    private readonly GetTafsiliLevelsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidAccountCodeId_Passes()
    {
        var result = _validator.Validate(new GetTafsiliLevelsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(new GetTafsiliLevelsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsiliLevelsQuery.AccountCodeId));
    }
}
