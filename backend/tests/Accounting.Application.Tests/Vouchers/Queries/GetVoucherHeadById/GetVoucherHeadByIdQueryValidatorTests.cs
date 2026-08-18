using Accounting.Application.Vouchers.Queries.GetVoucherHeadById;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherHeadById;

public sealed class GetVoucherHeadByIdQueryValidatorTests
{
    private readonly GetVoucherHeadByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetVoucherHeadByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadByIdQuery.Id));
    }
}
