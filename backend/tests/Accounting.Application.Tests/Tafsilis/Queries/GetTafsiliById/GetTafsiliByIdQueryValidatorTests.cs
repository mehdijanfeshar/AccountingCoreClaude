using Accounting.Application.Tafsilis.Queries.GetTafsiliById;

namespace Accounting.Application.Tests.Tafsilis.Queries.GetTafsiliById;

public sealed class GetTafsiliByIdQueryValidatorTests
{
    private readonly GetTafsiliByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new GetTafsiliByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new GetTafsiliByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }
}
