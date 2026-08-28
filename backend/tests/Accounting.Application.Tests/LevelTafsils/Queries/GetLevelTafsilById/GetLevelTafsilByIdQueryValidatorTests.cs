using Accounting.Application.LevelTafsils.Queries.GetLevelTafsilById;

namespace Accounting.Application.Tests.LevelTafsils.Queries.GetLevelTafsilById;

public sealed class GetLevelTafsilByIdQueryValidatorTests
{
    private readonly GetLevelTafsilByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetLevelTafsilByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetLevelTafsilByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetLevelTafsilByIdQuery.Id));
    }
}
