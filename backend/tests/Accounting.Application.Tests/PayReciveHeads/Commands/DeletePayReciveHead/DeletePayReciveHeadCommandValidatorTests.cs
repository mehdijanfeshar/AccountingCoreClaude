using Accounting.Application.PayReciveHeads.Commands.DeletePayReciveHead;

namespace Accounting.Application.Tests.PayReciveHeads.Commands.DeletePayReciveHead;

public sealed class DeletePayReciveHeadCommandValidatorTests
{
    private readonly DeletePayReciveHeadCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        Assert.True(_validator.Validate(new DeletePayReciveHeadCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        Assert.False(_validator.Validate(new DeletePayReciveHeadCommand(Guid.Empty)).IsValid);
    }
}
