using Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeadById;

namespace Accounting.Application.Tests.PayReciveHeads.Queries.GetPayReciveHeadById;

public sealed class GetPayReciveHeadByIdQueryValidatorTests
{
    private readonly GetPayReciveHeadByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        Assert.True(_validator.Validate(new GetPayReciveHeadByIdQuery(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        Assert.False(_validator.Validate(new GetPayReciveHeadByIdQuery(Guid.Empty)).IsValid);
    }
}
