using Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;

namespace Accounting.Application.Tests.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;

public sealed class GetTmpVoucherHeadByIdQueryValidatorTests
{
    private readonly GetTmpVoucherHeadByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        Assert.True(_validator.Validate(new GetTmpVoucherHeadByIdQuery(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        Assert.False(_validator.Validate(new GetTmpVoucherHeadByIdQuery(Guid.Empty)).IsValid);
    }
}
