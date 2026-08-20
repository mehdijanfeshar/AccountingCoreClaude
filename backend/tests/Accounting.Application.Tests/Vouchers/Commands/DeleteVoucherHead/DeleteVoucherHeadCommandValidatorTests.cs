using Accounting.Application.Vouchers.Commands.DeleteVoucherHead;

namespace Accounting.Application.Tests.Vouchers.Commands.DeleteVoucherHead;

public sealed class DeleteVoucherHeadCommandValidatorTests
{
    private readonly DeleteVoucherHeadCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteVoucherHeadCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteVoucherHeadCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteVoucherHeadCommand.Id));
    }
}
