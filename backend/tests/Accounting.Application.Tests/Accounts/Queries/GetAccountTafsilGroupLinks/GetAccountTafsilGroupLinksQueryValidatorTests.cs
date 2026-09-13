using Accounting.Application.Accounts.Queries.GetAccountTafsilGroupLinks;

namespace Accounting.Application.Tests.Accounts.Queries.GetAccountTafsilGroupLinks;

public sealed class GetAccountTafsilGroupLinksQueryValidatorTests
{
    private readonly GetAccountTafsilGroupLinksQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyAccountCodeId_Passes()
    {
        var result = _validator.Validate(new GetAccountTafsilGroupLinksQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var result = _validator.Validate(new GetAccountTafsilGroupLinksQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }
}
