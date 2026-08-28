using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;

namespace Accounting.Application.Tests.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;

public sealed class GetAttribForAccountCodeByIdQueryValidatorTests
{
    private readonly GetAttribForAccountCodeByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetAttribForAccountCodeByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetAttribForAccountCodeByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodeByIdQuery.Id));
    }
}
