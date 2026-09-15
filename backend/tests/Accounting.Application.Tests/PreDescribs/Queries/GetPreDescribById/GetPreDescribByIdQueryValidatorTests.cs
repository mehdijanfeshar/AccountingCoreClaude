using Accounting.Application.PreDescribs.Queries.GetPreDescribById;

namespace Accounting.Application.Tests.PreDescribs.Queries.GetPreDescribById;

public sealed class GetPreDescribByIdQueryValidatorTests
{
    private readonly GetPreDescribByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetPreDescribByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetPreDescribByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetPreDescribByIdQuery.Id));
    }
}
