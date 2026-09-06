using Accounting.Application.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;

namespace Accounting.Application.Tests.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;

public sealed class DeleteTmpVoucherHeadCommandValidatorTests
{
    private readonly DeleteTmpVoucherHeadCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        Assert.True(_validator.Validate(new DeleteTmpVoucherHeadCommand(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        Assert.False(_validator.Validate(new DeleteTmpVoucherHeadCommand(Guid.Empty)).IsValid);
    }
}
