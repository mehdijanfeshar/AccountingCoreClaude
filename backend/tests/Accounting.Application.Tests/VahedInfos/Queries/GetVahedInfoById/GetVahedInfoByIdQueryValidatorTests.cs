using Accounting.Application.VahedInfos.Queries.GetVahedInfoById;

namespace Accounting.Application.Tests.VahedInfos.Queries.GetVahedInfoById;

public sealed class GetVahedInfoByIdQueryValidatorTests
{
    private readonly GetVahedInfoByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetVahedInfoByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetVahedInfoByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVahedInfoByIdQuery.Id));
    }
}
