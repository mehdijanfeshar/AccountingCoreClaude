using Accounting.Application.BillLogs.Queries.GetBillLogById;

namespace Accounting.Application.Tests.BillLogs.Queries.GetBillLogById;

public sealed class GetBillLogByIdQueryValidatorTests
{
    private readonly GetBillLogByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetBillLogByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetBillLogByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBillLogByIdQuery.Id));
    }
}
