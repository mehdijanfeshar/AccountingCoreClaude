using Accounting.Application.Expenses.Queries.GetExpenseById;

namespace Accounting.Application.Tests.Expenses.Queries.GetExpenseById;

public sealed class GetExpenseByIdQueryValidatorTests
{
    private readonly GetExpenseByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetExpenseByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetExpenseByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetExpenseByIdQuery.Id));
    }
}
