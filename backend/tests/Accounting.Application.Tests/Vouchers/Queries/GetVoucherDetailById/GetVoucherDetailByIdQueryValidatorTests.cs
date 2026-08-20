using Accounting.Application.Vouchers.Queries.GetVoucherDetailById;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherDetailById;

public sealed class GetVoucherDetailByIdQueryValidatorTests
{
    private readonly GetVoucherDetailByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetVoucherDetailByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetVoucherDetailByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailByIdQuery.Id));
    }
}
