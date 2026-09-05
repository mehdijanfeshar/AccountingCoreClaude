using Accounting.Application.CheckBooks.Queries.GetCheckBookById;

namespace Accounting.Application.Tests.CheckBooks.Queries.GetCheckBookById;

public sealed class GetCheckBookByIdQueryValidatorTests
{
    private readonly GetCheckBookByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetCheckBookByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetCheckBookByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetCheckBookByIdQuery.Id));
    }
}
